﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Incrementals
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    using Newtonsoft.Json;

    internal class IncrementalBuildContext
    {
        private DocumentBuildParameters _parameters;
        private Dictionary<string, Dictionary<string, BuildPhase?>> _modelLoadInfo = new Dictionary<string, Dictionary<string, BuildPhase?>>();
        private Dictionary<string, ChangeKindWithDependency> _changeDict = new Dictionary<string, ChangeKindWithDependency>();

        public string BaseDir { get; }

        public string LastBaseDir { get; }

        public DateTime? LastBuildStartTime { get; }

        public BuildVersionInfo CurrentBuildVersionInfo { get; }

        public BuildVersionInfo LastBuildVersionInfo { get; }

        public bool CanVersionIncremental { get; }

        public string Version
        {
            get
            {
                return _parameters.VersionName;
            }
        }

        public IReadOnlyDictionary<string, Dictionary<string, BuildPhase?>> ModelLoadInfo
        {
            get
            {
                return _modelLoadInfo;
            }
        }

        public IReadOnlyDictionary<string, ChangeKindWithDependency> ChangeDict
        {
            get
            {
                return _changeDict;
            }
        }

        #region Creator and Constructor

        public static IncrementalBuildContext Create(DocumentBuildParameters parameters, BuildInfo cb, BuildInfo lb, string intermediateFolder)
        {
            var baseDir = Path.Combine(intermediateFolder, cb.DirectoryName);
            var lastBaseDir = lb != null ? Path.Combine(intermediateFolder, lb.DirectoryName) : null;
            var lastBuildStartTime = lb?.BuildStartTime;
            var canBuildInfoIncremental = CanBuildInfoIncremental(cb, lb);
            var lbv = lb?.Versions?.SingleOrDefault(v => v.VersionName == parameters.VersionName);
            var cbv = new BuildVersionInfo
            {
                VersionName = parameters.VersionName,
                ConfigHash = ComputeConfigHash(parameters),
                AttributesFile = IncrementalUtility.CreateRandomFileName(baseDir),
                DependencyFile = IncrementalUtility.CreateRandomFileName(baseDir),
                ManifestFile = IncrementalUtility.CreateRandomFileName(baseDir),
                XRefSpecMapFile = IncrementalUtility.CreateRandomFileName(baseDir),
                BuildMessageFile = IncrementalUtility.CreateRandomFileName(baseDir),
                Attributes = ComputeFileAttributes(parameters, lbv?.Dependency),
                Dependency = new DependencyGraph(),
            };
            cb.Versions.Add(cbv);
            var context = new IncrementalBuildContext(baseDir, lastBaseDir, lastBuildStartTime, canBuildInfoIncremental, parameters, cbv, lbv);
            return context;
        }

        private IncrementalBuildContext(string baseDir, string lastBaseDir, DateTime? lastBuildStartTime, bool canBuildInfoIncremental, DocumentBuildParameters parameters, BuildVersionInfo cbv, BuildVersionInfo lbv)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }
            _parameters = parameters;
            BaseDir = baseDir;
            LastBaseDir = lastBaseDir;
            LastBuildStartTime = lastBuildStartTime;
            CurrentBuildVersionInfo = cbv;
            LastBuildVersionInfo = lbv;
            CanVersionIncremental = canBuildInfoIncremental && GetCanVersionIncremental();
        }

        #endregion

        #region Model Load Info

        /// <summary>
        /// report model load info
        /// </summary>
        /// <param name="hostService">host service</param>
        /// <param name="file">the model's LocalPathFromRoot</param>
        /// <param name="phase">the buildphase that the model was loaded at</param>
        public void ReportModelLoadInfo(HostService hostService, string file, BuildPhase? phase)
        {
            Dictionary<string, BuildPhase?> mi = null;
            string name = hostService.Processor.Name;
            if (!_modelLoadInfo.TryGetValue(name, out mi))
            {
                _modelLoadInfo[name] = mi = new Dictionary<string, BuildPhase?>();
            }
            mi[file] = phase;
        }

        /// <summary>
        /// report model load info
        /// </summary>
        /// <param name="hostService">host service</param>
        /// <param name="files">models' LocalPathFromRoot</param>
        /// <param name="phase">the buildphase that the model was loaded at</param>
        public void ReportModelLoadInfo(HostService hostService, IEnumerable<string> files, BuildPhase? phase)
        {
            foreach (var f in files)
            {
                ReportModelLoadInfo(hostService, f, phase);
            }
        }

        public IReadOnlyDictionary<string, BuildPhase?> GetModelLoadInfo(HostService hostService)
        {
            string name = hostService.Processor.Name;
            Dictionary<string, BuildPhase?> mi;
            if (ModelLoadInfo.TryGetValue(name, out mi))
            {
                return mi;
            }
            return new Dictionary<string, BuildPhase?>();
        }

        #endregion

        #region Intermediate Files
        public ModelManifest GetCurrentIntermediateModelManifest(HostService hostService)
        {
            return CurrentBuildVersionInfo?.Processors?.Find(p => p.Name == hostService.Processor.Name)?.IntermediateModelManifest;
        }

        public ModelManifest GetLastIntermediateModelManifest(HostService hostService)
        {
            return LastBuildVersionInfo?.Processors?.Find(p => p.Name == hostService.Processor.Name)?.IntermediateModelManifest;
        }

        #endregion

        #region Changes and DependencyGraph

        public void LoadChanges()
        {
            if (LastBuildVersionInfo == null)
            {
                throw new InvalidOperationException("Only incremental build could load changes.");
            }
            if (_parameters.Changes != null)
            {
                // use user-provided changelist
                foreach (var pair in _parameters.Changes)
                {
                    _changeDict[pair.Key] = pair.Value;
                }
            }
            else
            {
                // get changelist from lastBuildInfo if user doesn't provide changelist
                var lastFileAttributes = LastBuildVersionInfo.Attributes;
                var fileAttributes = CurrentBuildVersionInfo.Attributes;
                DateTime checkTime = LastBuildStartTime.Value;
                foreach (var file in fileAttributes.Keys.Intersect(lastFileAttributes.Keys))
                {
                    var last = lastFileAttributes[file];
                    var current = fileAttributes[file];
                    if (current.LastModifiedTime > checkTime && current.MD5 != last.MD5)
                    {
                        _changeDict[file] = ChangeKindWithDependency.Updated;
                    }
                    else
                    {
                        _changeDict[file] = ChangeKindWithDependency.None;
                    }
                }

                foreach (var file in lastFileAttributes.Keys.Except(fileAttributes.Keys))
                {
                    _changeDict[file] = ChangeKindWithDependency.Deleted;
                }
                foreach (var file in fileAttributes.Keys.Except(lastFileAttributes.Keys))
                {
                    _changeDict[file] = ChangeKindWithDependency.Created;
                }
            }
        }

        public List<string> ExpandDependency(DependencyGraph dependencyGraph, Func<DependencyItem, bool> isValid)
        {
            var newChanges = new List<string>();

            if (dependencyGraph != null)
            {
                foreach (var from in dependencyGraph.FromNodes)
                {
                    if (dependencyGraph.GetAllDependencyFrom(from).Any(d => isValid(d) && _changeDict.ContainsKey(d.To) && _changeDict[d.To] != ChangeKindWithDependency.None))
                    {
                        if (!_changeDict.ContainsKey(from))
                        {
                            _changeDict[from] = ChangeKindWithDependency.DependencyUpdated;
                            newChanges.Add(from);
                        }
                        else
                        {
                            if (_changeDict[from] == ChangeKindWithDependency.None)
                            {
                                newChanges.Add(from);
                            }
                            _changeDict[from] |= ChangeKindWithDependency.DependencyUpdated;
                        }
                    }
                }
            }
            return newChanges;
        }

        #endregion

        #region BuildVersionInfo

        public void UpdateBuildVersionInfoPerDependencyGraph()
        {
            if (CurrentBuildVersionInfo.Dependency == null)
            {
                return;
            }
            var fileAttributes = CurrentBuildVersionInfo.Attributes;
            var nodesToUpdate = CurrentBuildVersionInfo.Dependency.GetAllDependentNodes().Except(fileAttributes.Keys);
            foreach (var node in nodesToUpdate)
            {
                string fullPath = Path.Combine(EnvironmentContext.BaseDirectory, ((RelativePath)node).RemoveWorkingFolder());
                if (File.Exists(fullPath))
                {
                    fileAttributes[node] = new FileAttributeItem
                    {
                        File = node,
                        LastModifiedTime = File.GetLastWriteTimeUtc(fullPath),
                        MD5 = StringExtension.GetMd5String(File.ReadAllText(fullPath)),
                    };
                }
            }
        }

        #endregion

        #region Private Methods

        private static string ComputeConfigHash(DocumentBuildParameters parameter)
        {
            return StringExtension.GetMd5String(JsonConvert.SerializeObject(
                parameter,
                new JsonSerializerSettings
                {
                    ContractResolver = new IncrementalCheckPropertiesResolver()
                }));
        }

        private static Dictionary<string, FileAttributeItem> ComputeFileAttributes(DocumentBuildParameters parameters, DependencyGraph dg)
        {
            var filesInScope = from f in parameters.Files.EnumerateFiles()
                               let fileKey = ((RelativePath)f.File).GetPathFromWorkingFolder().ToString()
                               select new
                               {
                                   PathFromWorkingFolder = fileKey,
                                   FullPath = f.FullPath
                               };
            var files = filesInScope;
            if (dg != null)
            {
                var filesFromDependency = from node in dg.GetAllDependentNodes()
                                          let fullPath = Path.Combine(EnvironmentContext.BaseDirectory, ((RelativePath)node).RemoveWorkingFolder())
                                          select new
                                          {
                                              PathFromWorkingFolder = node,
                                              FullPath = fullPath
                                          };
                files = files.Concat(filesFromDependency);
            }


            return (from item in files
                    where File.Exists(item.FullPath)
                    group item by item.PathFromWorkingFolder into g
                    select new FileAttributeItem
                    {
                        File = g.Key,
                        LastModifiedTime = File.GetLastWriteTimeUtc(g.First().FullPath),
                        MD5 = StringExtension.GetMd5String(File.ReadAllText(g.First().FullPath)),
                    }).ToDictionary(a => a.File);
        }

        private static bool CanBuildInfoIncremental(BuildInfo cb, BuildInfo lb)
        {
            if (lb == null)
            {
                return false;
            }
            if (cb.DocfxVersion != lb.DocfxVersion)
            {
                Logger.LogVerbose($"Cannot build incrementally because docfx version changed from {lb.DocfxVersion} to {cb.DocfxVersion}.");
                return false;
            }
            if (cb.PluginHash != lb.PluginHash)
            {
                Logger.LogVerbose("Cannot build incrementally because plugin changed.");
                return false;
            }
            if (cb.TemplateHash != lb.TemplateHash)
            {
                Logger.LogVerbose("Cannot build incrementally because template changed.");
                return false;
            }
            if (cb.CommitFromSHA != lb.CommitToSHA)
            {
                Logger.LogVerbose($"Cannot build incrementally because commit SHA doesn't match. Last build commit: {lb.CommitToSHA}. Current build commit base: {cb.CommitFromSHA}.");
                return false;
            }
            return true;
        }

        private bool GetCanVersionIncremental()
        {
            if (LastBuildVersionInfo == null)
            {
                Logger.LogVerbose($"Cannot build incrementally because last build didn't contain version {Version}.");
                return false;
            }
            if (CurrentBuildVersionInfo.ConfigHash != LastBuildVersionInfo.ConfigHash)
            {
                Logger.LogVerbose("Cannot build incrementally because config changed.");
                return false;
            }
            if (_parameters.ForceRebuild)
            {
                Logger.LogVerbose($"Disable incremental build by force rebuild option.");
                return false;
            }
            return true;
        }

        #endregion
    }
}

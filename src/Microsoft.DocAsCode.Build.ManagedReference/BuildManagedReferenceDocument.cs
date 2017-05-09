﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.ManagedReference
{
    using System.Collections.Generic;
    using System.Composition;
    using System.Linq;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.DataContracts.ManagedReference;
    using Microsoft.DocAsCode.Plugins;

    [Export(nameof(ManagedReferenceDocumentProcessor), typeof(IDocumentBuildStep))]
    public class BuildManagedReferenceDocument : BuildReferenceDocumentBase, ISupportIncrementalBuildStep
    {
        public override string Name => nameof(BuildManagedReferenceDocument);

        #region BuildReferenceDocumentBase

        protected override void BuildArticle(IHostService host, FileModel model)
        {
            var pageViewModel = (PageViewModel)model.Content;

            BuildArticleCore(host, model, shouldSkipMarkup: pageViewModel?.ShouldSkipMarkup ?? false);

            foreach (var r in pageViewModel.References)
            {
                if (r.IsExternal == false)
                {
                    host.ReportDependencyTo(model, r.Uid, DependencyItemSourceType.Uid, DependencyTypeName.Reference);
                }
            }

            foreach (var child in from i in pageViewModel.Items
                                  from c in i.Children ?? Enumerable.Empty<string>()
                                  select c)
            {
                host.ReportDependencyTo(model, child, DependencyItemSourceType.Uid, DependencyTypeName.Children);
            }
        }

        #endregion

        #region ISupportIncrementalBuildStep Members

        public bool CanIncrementalBuild(FileAndType fileAndType) => true;

        public string GetIncrementalContextHash() => null;

        public IEnumerable<DependencyType> GetDependencyTypesToRegister() => new[]
        {
            new DependencyType()
            {
                Name = DependencyTypeName.Reference,
                Phase = BuildPhase.Link,
                Transitivity = DependencyTransitivity.None,
            },
            new DependencyType()
            {
                Name = DependencyTypeName.Children,
                Phase = BuildPhase.Link,
                Transitivity = DependencyTransitivity.All,
            }
        };

        #endregion
    }
}

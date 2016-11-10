﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Utility;

    using TypeForwardedToPathUtility = Microsoft.DocAsCode.Common.PathUtility;
    using TypeForwardedToRelativePath = Microsoft.DocAsCode.Common.RelativePath;

    public class TemplateUtility
    {
        private readonly DocumentBuildContext _context;

        public TemplateUtility(DocumentBuildContext context)
        {
            _context = context;
        }

        public string ResolveSourceRelativePath(string originPath, string currentFileOutputPath)
        {
            if (string.IsNullOrEmpty(originPath) || !TypeForwardedToPathUtility.IsRelativePath(originPath))
            {
                return originPath;
            }

            var origin = (TypeForwardedToRelativePath)originPath;
            if (origin == null)
            {
                return originPath;
            }

            var destPath = _context.GetFilePath(origin.GetPathFromWorkingFolder().ToString());
            if (destPath != null)
            {
                return ((TypeForwardedToRelativePath)destPath - ((TypeForwardedToRelativePath)currentFileOutputPath).GetPathFromWorkingFolder()).ToString();
            }
            else
            {
                Logger.LogWarning($"Can't find output file for {originPath}");
                return originPath;
            }
        }
    }
}

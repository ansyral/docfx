namespace Microsoft.DocAsCode.Build.Engine.Incrementals
{
    using System;

    using Microsoft.DocAsCode.Common;

    public class FileItem : IDependencyItemSourceInfo, IEquatable<FileItem>
    {
        public override StringComparer ValueComparer => FilePathComparer.OSPlatformSensitiveStringComparer;

        public override DependencyItemSourceType SourceType => DependencyItemSourceType.File;

        public static implicit operator FileItem(string info)
        {
            return info == null ? null : new FileItem { Value = info };
        }
    }
}

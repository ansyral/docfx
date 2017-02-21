namespace Microsoft.DocAsCode.Build.Engine.Incrementals
{
    using System;

    public class ReferenceItem : DependencyItemSourceInfo, IEquatable<DependencyItemSourceInfo>
    {
        public override StringComparer ValueComparer => StringComparer.Ordinal;

        public override DependencyItemSourceType SourceType => DependencyItemSourceType.Reference;

        public string ReferenceType { get; set; }
    }
}

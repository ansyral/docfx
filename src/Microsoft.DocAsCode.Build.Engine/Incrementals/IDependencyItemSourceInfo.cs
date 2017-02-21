namespace Microsoft.DocAsCode.Build.Engine.Incrementals
{
    using System;

    public interface IDependencyItemSourceInfo
    {
        StringComparer ValueComparer { get; }

        DependencyItemSourceType SourceType { get; }

        string Value { get; set; }
    }
}

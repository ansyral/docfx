namespace Microsoft.DocAsCode.Build.Engine.Incrementals
{
    using System.Collections.Generic;

    using Microsoft.DocAsCode.Plugins;

    public class BuildMessage : Dictionary<BuildPhase, BuildMessageInfo>()
    {
    }
}

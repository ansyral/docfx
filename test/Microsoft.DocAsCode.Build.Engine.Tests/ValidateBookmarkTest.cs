namespace Microsoft.DocAsCode.Build.Engine.Tests
{
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.Plugins;

    using Xunit;

    public class ValidateBookmarkTest
    {
        private static ValidateBookmark _validator = new ValidateBookmark();

        [Fact]
        public void TestBasicFeature()
        {
            var outputFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "validate_bookmark");
            Directory.CreateDirectory(outputFolder);
            Manifest manifest = new Manifest
            {
                SourceBasePath = outputFolder,
                Files = new List<ManifestItem>
                {
                    new ManifestItem { SourceRelativePath = Path.Combine(outputFolder, "a.md"), OutputFiles = new Dictionary<string, OutputFileInfo> { { ".html", new OutputFileInfo { RelativePath = "a.html" } } } },
                    new ManifestItem { SourceRelativePath = Path.Combine(outputFolder, "b.md"), OutputFiles = new Dictionary<string, OutputFileInfo> { { ".html", new OutputFileInfo { RelativePath = "b.html" } } } },
                }
            };
        }
    }
}

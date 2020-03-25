using System.IO;
using Xunit;

namespace DocsPortingTool.Tests
{
    public class Tests
    {
        [Fact]
        public void PortRemarks() => Port("Remarks", false);

        [Fact]
        public void PortEII() => Port("EII", true);

        private void Port(string testDataDir, bool includeInterface)
        {
            using TestDirectory tempDir = new TestDirectory();
            TestData testData = new TestData(tempDir, testDataDir, includeInterface: includeInterface);

            Configuration config = new Configuration
            {
                DisablePrompts = true,
                PrintUndoc = false,
                Save = true,
                SkipExceptions = true,
                SkipInterfaceImplementations = !includeInterface,
                SkipRemarks = false
            };
            config.IncludedAssemblies.Add(testData.Assembly);
            config.DirsDocsXml.Add(testData.Docs);
            config.DirsTripleSlashXmls.Add(testData.TripleSlash);

            Analyzer analyzer = new Analyzer(config);
            analyzer.Start();

            string[] expectedLines = File.ReadAllLines(testData.ExpectedFilePath);
            string[] actualLines = File.ReadAllLines(testData.ActualFilePath);

            Assert.Equal(expectedLines.Length, actualLines.Length);

            for (int i = 0; i < expectedLines.Length; i++)
            {
                string expectedLine = expectedLines[i];
                string actualLine = actualLines[i];
                Assert.Equal(expectedLine, actualLine);
            }
        }

    }
}

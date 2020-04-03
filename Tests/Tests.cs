using System.IO;
using Xunit;

namespace DocsPortingTool.Tests
{
    public class Tests
    {
        [Fact]
        // Verifies that comments are ported, excluding EII comments.
        public void Port_Remarks()
        {
            Port("Remarks", GetConfig(
                skipInterfaceImplementations: true,
                skipInterfaceRemarks: true,
                skipRemarks: false
            ));
        }

        [Fact]
        // Verifies that EII comments are ported, including EII remarks.
        public void Port_EII()
        {
            Port("EII", GetConfig(
                skipInterfaceImplementations: false,
                skipInterfaceRemarks: false,
                skipRemarks: false
            ));
        }

        [Fact]
        /// Verifies that EII comments are ported, except EII remarks.
        public void Port_EII_NoRemarks()
        {
            Port("EII_NoRemarks", GetConfig(
                skipInterfaceImplementations: false,
                skipInterfaceRemarks: true,
                skipRemarks: false
            ));
        }

        private Configuration GetConfig(
            bool disablePrompts = true,
            bool printUndoc = false,
            bool save = true,
            bool skipExceptions = true,
            bool skipInterfaceImplementations = true,
            bool skipInterfaceRemarks = true,
            bool skipRemarks = true
        )
        {
            return new Configuration
            {
                DisablePrompts = disablePrompts,
                PrintUndoc = printUndoc,
                Save = save,
                SkipExceptions = skipExceptions,
                SkipInterfaceImplementations = skipInterfaceImplementations,
                SkipInterfaceRemarks = skipInterfaceRemarks,
                SkipRemarks = skipRemarks
            };
        }

        private void Port(string testDataDir, Configuration c)
        {
            using TestDirectory tempDir = new TestDirectory();

            TestData testData = new TestData(
                tempDir,
                testDataDir,
                includeInterface: !c.SkipInterfaceImplementations
            );

            c.IncludedAssemblies.Add(testData.Assembly);
            c.DirsDocsXml.Add(testData.Docs);
            c.DirsTripleSlashXmls.Add(testData.TripleSlash);

            Analyzer analyzer = new Analyzer(c);
            analyzer.Start();

            Verify(testData);
        }

        private void Verify(TestData testData)
        {
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

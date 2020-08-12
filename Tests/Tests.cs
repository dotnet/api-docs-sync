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
                skipInterfaceRemarks: true
            ));
        }

        [Fact]
        // Verifies that EII comments are ported, including EII remarks.
        public void Port_EII()
        {
            Port("EII", GetConfig(
                skipInterfaceImplementations: false,
                skipInterfaceRemarks: false
            ));
        }

        [Fact]
        /// Verifies that EII comments are ported, except EII remarks.
        public void Port_EII_NoRemarks()
        {
            Port("EII_NoRemarks", GetConfig(
                skipInterfaceImplementations: false,
                skipInterfaceRemarks: true
            ));
        }

        [Fact]
        /// Verifies that exceptions are ported.
        public void Port_Exceptions()
        {
            Port("Exceptions", GetConfig(
                portMemberExceptions: true
            ));
        }

        [Fact]
        /// Verifies that when an exception has already been ported, but went through language review, does not get ported if its above the difference threshold.
        public void Port_Exception_ExistingCref()
        {
            Port("Exception_ExistingCref", GetConfig(
                portMemberExceptions: true,
                exceptionCollisionThreshold: 60
            ));
        }

        private Configuration GetConfig(
            bool disablePrompts = true,
            bool printUndoc = false,
            bool save = true,
            bool skipInterfaceImplementations = true,
            bool skipInterfaceRemarks = true,
            bool portTypeRemarks = true,
            bool portMemberRemarks = true,
            bool portMemberExceptions = false,
            int exceptionCollisionThreshold = 70
        )
        {
            return new Configuration
            {
                DisablePrompts = disablePrompts,
                PrintUndoc = printUndoc,
                Save = save,
                SkipInterfaceImplementations = skipInterfaceImplementations,
                SkipInterfaceRemarks = skipInterfaceRemarks,
                PortTypeRemarks = portTypeRemarks,
                PortMemberRemarks = portMemberRemarks,
                PortMemberExceptions = portMemberExceptions,
                ExceptionCollisionThreshold = exceptionCollisionThreshold
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

            for (int i = 0; i < expectedLines.Length; i++)
            {
                string expectedLine = expectedLines[i];
                string actualLine = actualLines[i];
                Assert.Equal(expectedLine, actualLine);
            }

            Assert.Equal(expectedLines.Length, actualLines.Length);
        }

    }
}

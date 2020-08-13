using System.IO;
using Xunit;

namespace DocsPortingTool.Tests
{
    public class Tests
    {
        [Fact]
        // Verifies the basic case of porting all regular fields.
        public void Port_Basic()
        {
            Port("Basic", GetConfig());
        }

        [Fact]
        // Ports Type remarks from triple slash.
        // Ports Method remarks from triple slash.
        // No interface strings should be ported.
        public void Port_Remarks_NoEII_NoInterfaceRemarks()
        {
            Port("Remarks_NoEII_NoInterfaceRemarks", GetConfig(
                skipInterfaceImplementations: true,
                skipInterfaceRemarks: true
            ));
        }

        [Fact]
        // Ports Type remarks from triple slash.
        // Ports Method remarks from triple slash.
        // Ports EII message and interface method remarks.
        public void Port_Remarks_WithEII_WithInterfaceRemarks()
        {
            Port("Remarks_WithEII_WithInterfaceRemarks", GetConfig(
                skipInterfaceImplementations: false,
                skipInterfaceRemarks: false
            ));
        }

        [Fact]
        // Ports Type remarks from triple slash.
        // Ports Method remarks from triple slash.
        // Ports EII message but no interface method remarks.
        public void Port_Remarks_WithEII_NoInterfaceRemarks()
        {
            Port("Remarks_WithEII_NoInterfaceRemarks", GetConfig(
                skipInterfaceImplementations: false,
                skipInterfaceRemarks: true
            ));
        }

        [Fact]
        /// Verifies that new exceptions are ported.
        public void Port_Exceptions()
        {
            Port("Exceptions", GetConfig());
        }

        [Fact]
        /// Verifies that when an exception has already been ported, but went through
        /// language review, does not get ported if its above the difference threshold.
        public void Port_Exception_ExistingCref()
        {
            Port("Exception_ExistingCref", GetConfig(
                portExceptionsExisting: true,
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
            bool portExceptionsExisting = false,
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
                PortExceptionsExisting = portExceptionsExisting,
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

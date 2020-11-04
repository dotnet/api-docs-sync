using System.Collections.Generic;
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
            Port("Basic");
        }

        [Fact]
        public void Port_DontAddMissingRemarks()
        {
            Port("DontAddMissingRemarks");
        }

        [Fact]
        // Verifies porting of APIs living in namespaces whose name match their assembly.
        public void Port_AssemblyAndNamespaceSame()
        {
            Port("AssemblyAndNamespaceSame");
        }

        [Fact]
        // Verifies porting of APIs living in namespaces whose name does not match their assembly.
        public void Port_AssemblyAndNamespaceDifferent()
        {
            Port("AssemblyAndNamespaceDifferent",
                assemblyName: "MyAssembly",
                namespaceName: "MyNamespace");
        }

        [Fact]
        // Ports Type remarks from triple slash.
        // Ports Method remarks from triple slash.
        // No interface strings should be ported.
        public void Port_Remarks_NoEII_NoInterfaceRemarks()
        {
            Port("Remarks_NoEII_NoInterfaceRemarks",
                skipInterfaceImplementations: true,
                skipInterfaceRemarks: true);
        }

        [Fact]
        // Ports Type remarks from triple slash.
        // Ports Method remarks from triple slash.
        // Ports EII message and interface method remarks.
        public void Port_Remarks_WithEII_WithInterfaceRemarks()
        {
            Port("Remarks_WithEII_WithInterfaceRemarks",
                skipInterfaceImplementations: false,
                skipInterfaceRemarks: false);
        }

        [Fact]
        // Ports Type remarks from triple slash.
        // Ports Method remarks from triple slash.
        // Ports EII message but no interface method remarks.
        public void Port_Remarks_WithEII_NoInterfaceRemarks()
        {
            Port("Remarks_WithEII_NoInterfaceRemarks",
                skipInterfaceImplementations: false,
                skipInterfaceRemarks: true);
        }

        [Fact]
        /// Verifies that new exceptions are ported.
        public void Port_Exceptions()
        {
            Port("Exceptions");
        }

        [Fact]
        /// Verifies that when an exception has already been ported, but went through
        /// language review, does not get ported if its above the difference threshold.
        public void Port_Exception_ExistingCref()
        {
            Port("Exception_ExistingCref",
                portExceptionsExisting: true,
                exceptionCollisionThreshold: 60);
        }

        private void Port(
            string testDataDir,
            bool disablePrompts = true,
            bool printUndoc = false,
            bool save = true,
            bool skipInterfaceImplementations = true,
            bool skipInterfaceRemarks = true,
            bool portTypeRemarks = true,
            bool portMemberRemarks = true,
            bool portExceptionsExisting = false,
            int exceptionCollisionThreshold = 70,
            string assemblyName = TestData.TestAssembly,
            string namespaceName = null, // Most namespaces have the same assembly name
            string typeName = TestData.TestType)
        {
            using TestDirectory tempDir = new TestDirectory();

            TestData testData = new TestData(
                tempDir,
                testDataDir,
                skipInterfaceImplementations: skipInterfaceImplementations,
                assemblyName: assemblyName,
                namespaceName: namespaceName,
                typeName: typeName
            );

            Configuration c = new Configuration
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

            c.IncludedAssemblies.Add(assemblyName);

            if (!string.IsNullOrEmpty(namespaceName))
            {
                c.IncludedNamespaces.Add(namespaceName);
            }

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

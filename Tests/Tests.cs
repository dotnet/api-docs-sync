using System.IO;
using Xunit;

namespace Libraries.Tests
{
    public class Tests
    {
        [Fact]
        // Verifies the basic case of porting all regular fields.
        public void Port_Basic()
        {
            PortToDocs("Basic");
        }

        [Fact]
        public void Port_DontAddMissingRemarks()
        {
            PortToDocs("DontAddMissingRemarks");
        }

        [Fact]
        // Verifies porting of APIs living in namespaces whose name match their assembly.
        public void Port_AssemblyAndNamespaceSame()
        {
            PortToDocs("AssemblyAndNamespaceSame");
        }

        [Fact]
        // Verifies porting of APIs living in namespaces whose name does not match their assembly.
        public void Port_AssemblyAndNamespaceDifferent()
        {
            PortToDocs("AssemblyAndNamespaceDifferent",
                assemblyName: "MyAssembly",
                namespaceName: "MyNamespace");
        }

        [Fact]
        // Ports Type remarks from IntelliSense xml.
        // Ports Method remarks from IntelliSense xml.
        // No interface strings should be ported.
        public void Port_Remarks_NoEII_NoInterfaceRemarks()
        {
            PortToDocs("Remarks_NoEII_NoInterfaceRemarks",
                skipInterfaceImplementations: true,
                skipInterfaceRemarks: true);
        }

        [Fact]
        // Ports Type remarks from IntelliSense xml.
        // Ports Method remarks from IntelliSense xml.
        // Ports EII message and interface method remarks.
        public void Port_Remarks_WithEII_WithInterfaceRemarks()
        {
            PortToDocs("Remarks_WithEII_WithInterfaceRemarks",
                skipInterfaceImplementations: false,
                skipInterfaceRemarks: false);
        }

        [Fact]
        // Ports Type remarks from IntelliSense xml.
        // Ports Method remarks from IntelliSense xml.
        // Ports EII message but no interface method remarks.
        public void Port_Remarks_WithEII_NoInterfaceRemarks()
        {
            PortToDocs("Remarks_WithEII_NoInterfaceRemarks",
                skipInterfaceImplementations: false,
                skipInterfaceRemarks: true);
        }

        [Fact]
        /// Verifies that new exceptions are ported.
        public void Port_Exceptions()
        {
            PortToDocs("Exceptions");
        }

        [Fact]
        /// Verifies that when an exception has already been ported, but went through
        /// language review, does not get ported if its above the difference threshold.
        public void Port_Exception_ExistingCref()
        {
            PortToDocs("Exception_ExistingCref",
                portExceptionsExisting: true,
                exceptionCollisionThreshold: 60);
        }

        private void PortToDocs(
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
            c.DirsIntelliSense.Add(testData.IntelliSenseAndDLL);

            var porter = new ToDocsPorter(c);
            porter.Start();

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

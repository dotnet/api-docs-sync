using System;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace DocsPortingTool.Libraries.Tests
{
    public class PortToDocsTests : BasePortTests
    {
        public PortToDocsTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        // Verifies the basic case of porting all regular fields.
        public void Port_Basic()
        {
            PortToDocs("Basic", GetConfiguration());
        }

        [Fact]
        public void Port_DontAddMissingRemarks()
        {
            PortToDocs("DontAddMissingRemarks", GetConfiguration());
        }

        [Fact]
        // Verifies porting of APIs living in namespaces whose name match their assembly.
        public void Port_AssemblyAndNamespaceSame()
        {
            PortToDocs("AssemblyAndNamespaceSame", GetConfiguration());
        }

        [Fact]
        // Verifies porting of APIs living in namespaces whose name does not match their assembly.
        public void Port_AssemblyAndNamespaceDifferent()
        {
            PortToDocs("AssemblyAndNamespaceDifferent",
                       GetConfiguration(),
                       namespaceNames: new[] { TestData.TestNamespace});
        }

        [Fact]
        // Ports Type remarks from IntelliSense xml.
        // Ports Method remarks from IntelliSense xml.
        // No interface strings should be ported.
        public void Port_Remarks_NoEII_NoInterfaceRemarks()
        {
            Configuration c = GetConfiguration(skipInterfaceImplementations: true, skipInterfaceRemarks: true);
            PortToDocs("Remarks_NoEII_NoInterfaceRemarks", c);
        }

        [Fact]
        // Ports Type remarks from IntelliSense xml.
        // Ports Method remarks from IntelliSense xml.
        // Ports EII message and interface method remarks.
        public void Port_Remarks_WithEII_WithInterfaceRemarks()
        {
            Configuration c = GetConfiguration(skipInterfaceImplementations: false, skipInterfaceRemarks: false);
            PortToDocs("Remarks_WithEII_WithInterfaceRemarks", c);
        }

        [Fact]
        // Ports Type remarks from IntelliSense xml.
        // Ports Method remarks from IntelliSense xml.
        // Ports EII message but no interface method remarks.
        public void Port_Remarks_WithEII_NoInterfaceRemarks()
        {
            Configuration c = GetConfiguration(skipInterfaceImplementations: false, skipInterfaceRemarks: true);
            PortToDocs("Remarks_WithEII_NoInterfaceRemarks", c);
        }

        [Fact]
        // Verifies that new exceptions are ported.
        public void Port_Exceptions()
        {
            PortToDocs("Exceptions", GetConfiguration());
        }

        [Fact]
        // Verifies that when an exception has already been ported, but went through
        // language review, does not get ported if its above the difference threshold.
        public void Port_Exception_ExistingCref()
        {
            Configuration c = GetConfiguration(portExceptionsExisting: true, exceptionCollisionThreshold: 60);
            PortToDocs("Exception_ExistingCref", c);
        }

        [Fact]
        // Avoid porting enum field remarks
        public void Port_EnumRemarks()
        {
            PortToDocs("EnumRemarks", GetConfiguration());
        }

        [Fact]
        // When the inheritdoc label is found, look for the parent type's documentation.
        // The parent type is located in a different assembly.
        public void Port_InheritDoc()
        {
            PortToDocs("InheritDoc",
                       GetConfiguration(),
                       assemblyNames: new[] { TestData.TestAssembly, "System" });
        }

        private static readonly string TestDataRootDir = Path.Join("..", "..", "..", "PortToDocs", "TestData");
        private static readonly string IntellisenseDir = "intellisense";
        private static readonly string XmlExpectedDir = "xml_expected";
        private static readonly string XmlActualDir = "xml";

        private static void DirectoryRecursiveCopy(string sourceDir, string targetDir)
        {
            DirectoryInfo dir = new(sourceDir);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException($"Source directory does not exist or could not be found: {sourceDir}");
            }

            Directory.CreateDirectory(targetDir);

            foreach (FileInfo file in dir.GetFiles())
            {
                string tempPath = Path.Combine(targetDir, file.Name);
                file.CopyTo(tempPath, false);
            }

            foreach (DirectoryInfo subdir in dir.GetDirectories())
            {
                string tempPath = Path.Combine(targetDir, subdir.Name);
                DirectoryRecursiveCopy(subdir.FullName, tempPath);
            }
        }

        private static Configuration GetConfiguration(
            bool disablePrompts = true,
            bool printUndoc = false,
            bool save = true,
            bool skipInterfaceImplementations = true,
            bool skipInterfaceRemarks = true,
            bool portTypeRemarks = true,
            bool portMemberRemarks = true,
            bool portExceptionsExisting = false,
            int exceptionCollisionThreshold = 70) => new()
            {
                DisablePrompts = disablePrompts,
                ExceptionCollisionThreshold = exceptionCollisionThreshold,
                PortExceptionsExisting = portExceptionsExisting,
                PortMemberRemarks = portMemberRemarks,
                PortTypeRemarks = portTypeRemarks,
                PrintUndoc = printUndoc,
                Save = save,
                SkipInterfaceImplementations = skipInterfaceImplementations,
                SkipInterfaceRemarks = skipInterfaceRemarks
            };

    private static void PortToDocs(
            string testName,
            Configuration c,
            string[] assemblyNames = null,
            string[] namespaceNames = null) // Most namespaces have the same assembly name
        {
            assemblyNames ??= new string[] { TestData.TestAssembly };
            namespaceNames ??= Array.Empty<string>();

            using TestDirectory testDirectory = new();

            string targetDir = Path.Join(testDirectory.FullPath, testName);
            Directory.CreateDirectory(targetDir);

            string sourceDir = Path.Join(TestDataRootDir, testName);

            DirectoryRecursiveCopy(sourceDir, targetDir);

            foreach (string assemblyName in assemblyNames)
            {
                c.IncludedAssemblies.Add(assemblyName);
            }

            foreach (string namespaceName in namespaceNames)
            {
                c.IncludedNamespaces.Add(namespaceName);
            }

            c.DirsDocsXml.Add(new(Path.Join(targetDir, XmlActualDir)));
            c.DirsIntelliSense.Add(new(Path.Join(targetDir, IntellisenseDir)));

            var porter = new ToDocsPorter(c);
            porter.Start();

            Verify(targetDir);
        }

        private static void Verify(string rootPath)
        {
            EnumerationOptions o = new() { RecurseSubdirectories = true };
            FileInfo[] expectedXmlFiles = new DirectoryInfo(Path.Join(rootPath, XmlExpectedDir)).GetFiles("*.xml", o);
            FileInfo[] actualXmlFiles = new DirectoryInfo(Path.Join(rootPath, XmlActualDir)).GetFiles("*.xml", o);

            foreach (var expectedFile in expectedXmlFiles)
            {
                FileInfo actualFile = actualXmlFiles.FirstOrDefault(x =>
                    x.Name == expectedFile.Name &&
                    Path.GetFileName(x.DirectoryName) == Path.GetFileName(expectedFile.DirectoryName));
                Assert.NotNull(actualFile);

                string expectedText = File.ReadAllText(expectedFile.FullName);
                string actualText = File.ReadAllText(actualFile.FullName);

                Assert.Equal(expectedText, actualText);
            }
        }
    }
}

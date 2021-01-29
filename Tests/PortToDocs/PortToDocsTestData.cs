using System.IO;
using Xunit;

namespace Libraries.Tests
{
    internal class PortToDocsTestData : TestData
    {
        private const string TestDataRootDirPath = @"../../../PortToDocs/TestData";
        private const string IntellisenseAndDllDirName = "IntelliSenseAndDLL";
        internal DirectoryInfo IntelliSenseAndDLLDir { get; set; }

        // Docs file with the interface from which the type inherits.
        internal string InterfaceFilePath { get; set; }

        internal string DocsOriginFilePath { get; set; }

        internal PortToDocsTestData(
            TestDirectory tempDir,
            string testDataDir,
            string assemblyName,
            string namespaceName,
            string typeName,
            bool skipInterfaceImplementations = true)
        {
            Assert.False(string.IsNullOrWhiteSpace(assemblyName));
            Assert.False(string.IsNullOrWhiteSpace(typeName));

            namespaceName = string.IsNullOrEmpty(namespaceName) ? assemblyName : namespaceName;

            IntelliSenseAndDLLDir = tempDir.CreateSubdirectory(IntellisenseAndDllDirName);
            Assert.True(IntelliSenseAndDLLDir.Exists, "Verify IntelliSense and DLL directory exists.");

            DirectoryInfo tripleSlashAssemblyDir = IntelliSenseAndDLLDir.CreateSubdirectory(assemblyName);
            Assert.True(tripleSlashAssemblyDir.Exists, "Verify triple slash and assembly directory exists.");

            DocsDir = tempDir.CreateSubdirectory(DocsDirName);
            Assert.True(DocsDir.Exists, "Verify docs directory exists.");

            DirectoryInfo docsAssemblyDir = DocsDir.CreateSubdirectory(namespaceName);
            Assert.True(docsAssemblyDir.Exists, "Verify docs assembly directory exists.");

            string testDataPath = Path.Combine(TestDataRootDirPath, testDataDir);

            string tripleSlashOriginalFilePath = Path.Combine(testDataPath, "TSOriginal.xml");
            string docsOriginalFilePath = Path.Combine(testDataPath, "DocsOriginal.xml");
            string docsExpectedFilePath = Path.Combine(testDataPath, "DocsExpected.xml");

            Assert.True(File.Exists(tripleSlashOriginalFilePath), "Verify triple slash original file exists.");
            Assert.True(File.Exists(docsOriginalFilePath), "Verify docs original file exists.");
            Assert.True(File.Exists(docsExpectedFilePath), "Verify docs expected file exists.");

            DocsOriginFilePath = Path.Combine(tripleSlashAssemblyDir.FullName, $"{typeName}.xml");
            ActualFilePath = Path.Combine(docsAssemblyDir.FullName, $"{typeName}.xml");
            ExpectedFilePath = Path.Combine(tempDir.FullPath, "DocsExpected.xml");

            File.Copy(tripleSlashOriginalFilePath, DocsOriginFilePath);
            Assert.True(File.Exists(DocsOriginFilePath), "Verify triple slash original file (copied) exists.");

            File.Copy(docsOriginalFilePath, ActualFilePath);
            Assert.True(File.Exists(ActualFilePath), "Verify docs original file (copied) exists.");

            File.Copy(docsExpectedFilePath, ExpectedFilePath);
            Assert.True(File.Exists(ExpectedFilePath), "Verify docs expected file (copied) exists.");

            if (!skipInterfaceImplementations)
            {
                string interfaceFilePath = Path.Combine(testDataPath, "DocsInterface.xml");
                Assert.True(File.Exists(interfaceFilePath), "Verify docs interface file exists.");

                string interfaceAssembly = "System";

                DirectoryInfo interfaceAssemblyDir = DocsDir.CreateSubdirectory(interfaceAssembly);
                Assert.True(interfaceAssemblyDir.Exists, "Verify interface assembly directory exists.");

                InterfaceFilePath = Path.Combine(interfaceAssemblyDir.FullName, "IMyInterface.xml");
                File.Copy(interfaceFilePath, InterfaceFilePath);
                Assert.True(File.Exists(InterfaceFilePath), "Verify docs interface file (copied) exists.");
            }
        }
    }

}

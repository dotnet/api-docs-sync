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
            DirectoryInfo tripleSlashAssemblyDir = IntelliSenseAndDLLDir.CreateSubdirectory(assemblyName);

            DocsDir = tempDir.CreateSubdirectory(DocsDirName);
            DirectoryInfo docsAssemblyDir = DocsDir.CreateSubdirectory(namespaceName);

            string testDataPath = Path.Combine(TestDataRootDirPath, testDataDir);

            string tripleSlashOriginalFilePath = Path.Combine(testDataPath, "TSOriginal.xml");
            string docsOriginalFilePath = Path.Combine(testDataPath, "DocsOriginal.xml");
            string docsExpectedFilePath = Path.Combine(testDataPath, "DocsExpected.xml");

            Assert.True(File.Exists(tripleSlashOriginalFilePath));
            Assert.True(File.Exists(docsOriginalFilePath));
            Assert.True(File.Exists(docsExpectedFilePath));

            DocsOriginFilePath = Path.Combine(tripleSlashAssemblyDir.FullName, $"{typeName}.xml");
            ActualFilePath = Path.Combine(docsAssemblyDir.FullName, $"{typeName}.xml");
            ExpectedFilePath = Path.Combine(tempDir.FullPath, "DocsExpected.xml");

            File.Copy(tripleSlashOriginalFilePath, DocsOriginFilePath);
            File.Copy(docsOriginalFilePath, ActualFilePath);
            File.Copy(docsExpectedFilePath, ExpectedFilePath);

            Assert.True(File.Exists(DocsOriginFilePath));
            Assert.True(File.Exists(ActualFilePath));
            Assert.True(File.Exists(ExpectedFilePath));

            if (!skipInterfaceImplementations)
            {
                string interfaceFilePath = Path.Combine(testDataPath, "DocsInterface.xml");
                Assert.True(File.Exists(interfaceFilePath));

                string interfaceAssembly = "System";
                DirectoryInfo interfaceAssemblyDir = DocsDir.CreateSubdirectory(interfaceAssembly);
                InterfaceFilePath = Path.Combine(interfaceAssemblyDir.FullName, "IMyInterface.xml");
                File.Copy(interfaceFilePath, InterfaceFilePath);
                Assert.True(File.Exists(InterfaceFilePath));
            }
        }
    }

}

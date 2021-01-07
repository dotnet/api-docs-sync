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

            Assembly = assemblyName;
            Namespace = string.IsNullOrEmpty(namespaceName) ? assemblyName : namespaceName;
            Type = typeName;

            IntelliSenseAndDLLDir = tempDir.CreateSubdirectory(IntellisenseAndDllDirName);
            DirectoryInfo tripleSlashAssemblyDir = IntelliSenseAndDLLDir.CreateSubdirectory(Assembly);

            DocsDir = tempDir.CreateSubdirectory(DocsDirName);
            DirectoryInfo docsAssemblyDir = DocsDir.CreateSubdirectory(Namespace);

            string testDataPath = Path.Combine(TestDataRootDirPath, testDataDir);

            string tripleSlashOriginalFilePath = Path.Combine(testDataPath, "TSOriginal.xml");
            string docsOriginalFilePath = Path.Combine(testDataPath, "DocsOriginal.xml");
            string docsExpectedFilePath = Path.Combine(testDataPath, "DocsExpected.xml");

            Assert.True(File.Exists(tripleSlashOriginalFilePath));
            Assert.True(File.Exists(docsOriginalFilePath));
            Assert.True(File.Exists(docsExpectedFilePath));

            OriginalFilePath = Path.Combine(tripleSlashAssemblyDir.FullName, $"{Type}.xml");
            ActualFilePath = Path.Combine(docsAssemblyDir.FullName, $"{Type}.xml");
            ExpectedFilePath = Path.Combine(tempDir.FullPath, "DocsExpected.xml");

            File.Copy(tripleSlashOriginalFilePath, OriginalFilePath);
            File.Copy(docsOriginalFilePath, ActualFilePath);
            File.Copy(docsExpectedFilePath, ExpectedFilePath);

            Assert.True(File.Exists(OriginalFilePath));
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

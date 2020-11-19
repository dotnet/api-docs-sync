using System.IO;
using Xunit;

namespace Libraries.Tests
{
    public class TestData
    {
        private string TestDataRootDir => @"..\..\..\TestData";

        public const string TestAssembly = "MyAssembly";
        public const string TestNamespace = "MyNamespace";
        public const string TestType = "MyType";

        public string Assembly { get; private set; }
        public string Namespace { get; private set; }
        public string Type { get; private set; }
        public DirectoryInfo IntelliSenseAndDLL { get; private set; }
        public DirectoryInfo Docs { get; private set; }

        /// <summary>IntelliSense xml file.</summary>
        public string OriginalFilePath { get; private set; }
        /// <summary>Docs file as we should expect it to look.</summary>
        public string ExpectedFilePath { get; private set; }
        /// <summary>Docs file the tool will modify.</summary>
        public string ActualFilePath { get; private set; }
        /// <summary>Docs file with the interface from which the type inherits.</summary>
        public string InterfaceFilePath { get; private set; }

        public TestData(TestDirectory tempDir, string testDataDir, string assemblyName, string namespaceName, string typeName, bool skipInterfaceImplementations = true)
        {
            Assert.False(string.IsNullOrWhiteSpace(assemblyName));
            Assert.False(string.IsNullOrWhiteSpace(typeName));

            Assembly = assemblyName;
            Namespace = string.IsNullOrEmpty(namespaceName) ? assemblyName : namespaceName;
            Type = typeName;

            IntelliSenseAndDLL = tempDir.CreateSubdirectory("IntelliSenseAndDLL");
            DirectoryInfo tsAssemblyDir = IntelliSenseAndDLL.CreateSubdirectory(Assembly);

            Docs = tempDir.CreateSubdirectory("Docs");
            DirectoryInfo docsAssemblyDir = Docs.CreateSubdirectory(Namespace);

            string testDataPath = Path.Combine(TestDataRootDir, testDataDir);

            string tsOriginFilePath = Path.Combine(testDataPath, "TSOriginal.xml");
            string docsOriginFilePath = Path.Combine(testDataPath, "DocsOriginal.xml");
            string docsOriginExpectedFilePath = Path.Combine(testDataPath, "DocsExpected.xml");

            Assert.True(File.Exists(tsOriginFilePath));
            Assert.True(File.Exists(docsOriginFilePath));
            Assert.True(File.Exists(docsOriginExpectedFilePath));

            OriginalFilePath = Path.Combine(tsAssemblyDir.FullName, $"{Type}.xml");
            ActualFilePath = Path.Combine(docsAssemblyDir.FullName, $"{Type}.xml");
            ExpectedFilePath = Path.Combine(tempDir.FullPath, "DocsExpected.xml");

            File.Copy(tsOriginFilePath, OriginalFilePath);
            File.Copy(docsOriginFilePath, ActualFilePath);
            File.Copy(docsOriginExpectedFilePath, ExpectedFilePath);

            Assert.True(File.Exists(OriginalFilePath));
            Assert.True(File.Exists(ActualFilePath));
            Assert.True(File.Exists(ExpectedFilePath));

            if (!skipInterfaceImplementations)
            {
                string interfaceFilePath = Path.Combine(testDataPath, "DocsInterface.xml");
                Assert.True(File.Exists(interfaceFilePath));

                string interfaceAssembly = "System";
                DirectoryInfo interfaceAssemblyDir = Docs.CreateSubdirectory(interfaceAssembly);
                InterfaceFilePath = Path.Combine(interfaceAssemblyDir.FullName, "IMyInterface.xml");
                File.Copy(interfaceFilePath, InterfaceFilePath);
                Assert.True(File.Exists(InterfaceFilePath));
            }
        }
    }

}

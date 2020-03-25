using System.IO;
using Xunit;

namespace DocsPortingTool.Tests
{
    public class TestData
    {
        private string TestDataRootDir => @"..\..\..\TestData";

        public string Assembly { get; private set; }
        public string Type { get; private set; }
        public DirectoryInfo TripleSlash { get; private set; }
        public DirectoryInfo Docs { get; private set; }

        /// <summary>Triple slash xml file.</summary>
        public string OriginalFilePath { get; private set; }
        /// <summary>Docs file as we should expect it to look.</summary>
        public string ExpectedFilePath { get; private set; }
        /// <summary>Docs file the tool will modify.</summary>
        public string ActualFilePath { get; private set; }
        /// <summary>Docs file with the interface from which the type inherits.</summary>
        public string InterfaceFilePath { get; private set; }

        public TestData(TestDirectory tempDir, string testDataDir, string assemblyName = "MyAssembly", string typeName = "MyType", bool includeInterface = false)
        {
            Assembly = assemblyName;
            Type = typeName;

            TripleSlash = tempDir.CreateSubdirectory("TripleSlash");
            DirectoryInfo tsAssemblyDir = TripleSlash.CreateSubdirectory(assemblyName);

            Docs = tempDir.CreateSubdirectory("Docs");
            DirectoryInfo docsAssemblyDir = Docs.CreateSubdirectory(assemblyName);

            string testDataPath = Path.Combine(TestDataRootDir, testDataDir);

            string tsOriginFilePath = Path.Combine(testDataPath, "TSOriginal.xml");
            string docsOriginFilePath = Path.Combine(testDataPath, "DocsOriginal.xml");
            string docsOriginExpectedFilePath = Path.Combine(testDataPath, "DocsExpected.xml");

            Assert.True(File.Exists(tsOriginFilePath));
            Assert.True(File.Exists(docsOriginFilePath));
            Assert.True(File.Exists(docsOriginExpectedFilePath));

            OriginalFilePath = Path.Combine(tsAssemblyDir.FullName, $"{typeName}.xml");
            ActualFilePath = Path.Combine(docsAssemblyDir.FullName, $"{typeName}.xml");
            ExpectedFilePath = Path.Combine(tempDir.FullPath, "DocsExpected.xml");

            File.Copy(tsOriginFilePath, OriginalFilePath);
            File.Copy(docsOriginFilePath, ActualFilePath);
            File.Copy(docsOriginExpectedFilePath, ExpectedFilePath);

            Assert.True(File.Exists(OriginalFilePath));
            Assert.True(File.Exists(ActualFilePath));
            Assert.True(File.Exists(ExpectedFilePath));

            if (includeInterface)
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

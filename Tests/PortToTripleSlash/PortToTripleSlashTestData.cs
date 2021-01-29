using System.IO;
using Xunit;

namespace Libraries.Tests
{
    internal class PortToTripleSlashTestData : TestData
    {
        private string TestDataRootDirPath => @"../../../PortToTripleSlash/TestData";
        private const string ProjectDirName = "Project";
        private DirectoryInfo ProjectDir { get; set; }
        internal string ProjectFilePath { get; set; }

        internal PortToTripleSlashTestData(
            TestDirectory tempDir,
            string testDataDir,
            string assemblyName,
            string namespaceName,
            string typeName)
        {
            Assert.False(string.IsNullOrWhiteSpace(assemblyName));
            Assert.False(string.IsNullOrWhiteSpace(typeName));

            namespaceName = string.IsNullOrEmpty(namespaceName) ? assemblyName : namespaceName;

            ProjectDir = tempDir.CreateSubdirectory(ProjectDirName);

            DocsDir = tempDir.CreateSubdirectory(DocsDirName);
            DirectoryInfo docsAssemblyDir = DocsDir.CreateSubdirectory(namespaceName);

            string testDataPath = Path.Combine(TestDataRootDirPath, testDataDir);

            foreach (string origin in Directory.EnumerateFiles(testDataPath, "*.xml"))
            {
                string fileName = Path.GetFileName(origin);
                string destination = Path.Combine(docsAssemblyDir.FullName, fileName);
                File.Copy(origin, destination);
            }

            string originCsOriginal = Path.Combine(testDataPath, $"SourceOriginal.cs");
            ActualFilePath = Path.Combine(ProjectDir.FullName, $"{typeName}.cs");
            File.Copy(originCsOriginal, ActualFilePath);

            string originCsExpected = Path.Combine(testDataPath, $"SourceExpected.cs");
            ExpectedFilePath = Path.Combine(tempDir.FullPath, $"SourceExpected.cs");
            File.Copy(originCsExpected, ExpectedFilePath);

            string originCsproj = Path.Combine(testDataPath, $"{assemblyName}.csproj");
            ProjectFilePath = Path.Combine(ProjectDir.FullName, $"{assemblyName}.csproj");
            File.Copy(originCsproj, ProjectFilePath);
        }
    }
}

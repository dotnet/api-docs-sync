using System;
using System.Collections.Generic;
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

            Assembly = assemblyName;
            Namespace = string.IsNullOrEmpty(namespaceName) ? assemblyName : namespaceName;
            Type = typeName;

            ProjectDir = tempDir.CreateSubdirectory(ProjectDirName);

            DocsDir = tempDir.CreateSubdirectory(DocsDirName);
            DirectoryInfo docsAssemblyDir = DocsDir.CreateSubdirectory(Namespace);

            string testDataPath = Path.Combine(TestDataRootDirPath, testDataDir);

            string docsOriginalFilePath = Path.Combine(testDataPath, "DocsOriginal.xml");
            string csOriginalFilePath = Path.Combine(testDataPath, "SourceOriginal.cs");
            string csExpectedFilePath = Path.Combine(testDataPath, "SourceExpected.cs");
            string csprojOriginalFilePath = Path.Combine(testDataPath, "Project.csproj");

            Assert.True(File.Exists(docsOriginalFilePath));
            Assert.True(File.Exists(csOriginalFilePath));
            Assert.True(File.Exists(csExpectedFilePath));
            Assert.True(File.Exists(csprojOriginalFilePath));

            OriginalFilePath = Path.Combine(docsAssemblyDir.FullName, $"{Type}.xml");
            ActualFilePath = Path.Combine(ProjectDir.FullName, $"{Type}.cs");
            ExpectedFilePath = Path.Combine(tempDir.FullPath, "SourceExpected.cs");
            ProjectFilePath = Path.Combine(ProjectDir.FullName, $"{Assembly}.csproj");

            File.Copy(docsOriginalFilePath, OriginalFilePath);
            File.Copy(csOriginalFilePath, ActualFilePath);
            File.Copy(csExpectedFilePath, ExpectedFilePath);
            File.Copy(csprojOriginalFilePath, ProjectFilePath);

            Assert.True(File.Exists(OriginalFilePath));
            Assert.True(File.Exists(ActualFilePath));
            Assert.True(File.Exists(ExpectedFilePath));
            Assert.True(File.Exists(ProjectFilePath));
        }
    }
}

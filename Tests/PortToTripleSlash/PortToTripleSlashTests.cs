using System.IO;
using Xunit;

namespace Libraries.Tests
{
    public class PortToTripleSlashTests
    {
        [Fact]
        public void Port_Basic()
        {
            PortToTripleSlash("Basic");
        }

        private void PortToTripleSlash(
            string testDataDir,
            bool save = true,
            string assemblyName = TestData.TestAssembly,
            string namespaceName = TestData.TestNamespace,
            string typeName = TestData.TestType)
        {
            using TestDirectory tempDir = new TestDirectory();

            PortToTripleSlashTestData testData = new PortToTripleSlashTestData(
                tempDir,
                testDataDir,
                assemblyName: assemblyName,
                namespaceName: namespaceName,
                typeName: typeName);

            Configuration c = new()
            {
                Direction = Configuration.PortingDirection.ToTripleSlash,
                CsProj = new FileInfo(testData.ProjectFilePath),
                Save = save
            };

            c.IncludedAssemblies.Add(assemblyName);

            if (!string.IsNullOrEmpty(namespaceName))
            {
                c.IncludedNamespaces.Add(namespaceName);
            }

            c.DirsDocsXml.Add(testData.DocsDir);

            var porter = new ToTripleSlashPorter(c);
            porter.Start();

            Verify(testData);
        }

        private void Verify(PortToTripleSlashTestData testData)
        {
            string[] expectedLines = File.ReadAllLines(testData.ExpectedFilePath);
            string[] actualLines = File.ReadAllLines(testData.ActualFilePath);

            for (int i = 0; i < expectedLines.Length; i++)
            {
                string expectedLine = expectedLines[i];
                string actualLine = actualLines[i];
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    if (expectedLine != actualLine)
                    {
                        System.Diagnostics.Debugger.Break();
                    }
                }
                Assert.Equal(expectedLine, actualLine);
            }

            Assert.Equal(expectedLines.Length, actualLines.Length);
        }
    }
}

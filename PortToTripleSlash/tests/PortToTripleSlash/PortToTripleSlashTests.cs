using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace DocsPortingTool.Libraries.Tests
{
    public class PortToTripleSlashTests : BasePortTests
    {
        public PortToTripleSlashTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void Port_Basic()
        {
            PortToTripleSlash("Basic");
        }

        [Fact]
        public void Port_Generics()
        {
            PortToTripleSlash("Generics");
        }

        private static void PortToTripleSlash(
            string testDataDir,
            bool save = true,
            bool skipInterfaceImplementations = true,
            string assemblyName = TestData.TestAssembly,
            string namespaceName = TestData.TestNamespace)
        {
            using TestDirectory tempDir = new();

            PortToTripleSlashTestData testData = new(
                tempDir,
                testDataDir,
                assemblyName,
                namespaceName);

            Configuration c = new()
            {
                CsProj = new FileInfo(testData.ProjectFilePath),
                Save = save,
                SkipInterfaceImplementations = skipInterfaceImplementations
            };

            c.IncludedAssemblies.Add(assemblyName);

            if (!string.IsNullOrEmpty(namespaceName))
            {
                c.IncludedNamespaces.Add(namespaceName);
            }

            c.DirsDocsXml.Add(testData.DocsDir);

            ToTripleSlashPorter.Start(c);

            Verify(testData);
        }

        private static void Verify(PortToTripleSlashTestData testData)
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

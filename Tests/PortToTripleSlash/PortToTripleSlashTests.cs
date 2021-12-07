using VerifyXunit;
using Xunit;
using Xunit.Abstractions;

namespace Libraries.Tests
{
    [UsesVerify]
    public class PortToTripleSlashTests : BasePortTests
    {
        public PortToTripleSlashTests(ITestOutputHelper output) 
            : base(output)
        {
        }

        [Theory]
        [InlineData("Basic")]
        [InlineData("Generics")]
        public async Task PortToTripleSlash(string scenario)
        {
            await TestScenario(scenario);
        }

        private static async Task TestScenario(
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
                Direction = Configuration.PortingDirection.ToTripleSlash,
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

            await Verifier.VerifyFile(testData.ActualFilePath)
                .UseDirectory($"./TestData/{testDataDir}")
                .UseFileName("SourceExpected");
        }
    }
}

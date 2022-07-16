// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ApiDocsSync.Libraries.Tests
{
    public class PortToTripleSlashTests : BasePortTests
    {
        public PortToTripleSlashTests(ITestOutputHelper output) : base(output)
        {
        }

        // Tests failing due to: https://github.com/dotnet/roslyn/issues/61454

        // Project.OpenProjectAsync - C:\Users\carlos\AppData\Local\Temp\dmeyjbwb.vtc\Project\MyAssembly.csproj
        // Failure - Msbuild failed when processing the file 'C:\Users\carlos\AppData\Local\Temp\dmeyjbwb.vtc\Project\MyAssembly.csproj' with message: C:\Program Files\dotnet\sdk\6.0.302\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.Sdk.FrameworkReferenceResolution.targets: (90, 5): The "ProcessFrameworkReferences" task failed unexpectedly.
        // System.IO.FileLoadException: Could not load file or assembly 'NuGet.Frameworks, Version=6.2.1.7, Culture=neutral, PublicKeyToken=31bf3856ad364e35'.Could not find or load a specific file. (0x80131621)
        // File name: 'NuGet.Frameworks, Version=6.2.1.7, Culture=neutral, PublicKeyToken=31bf3856ad364e35'

        [Fact]
        public Task Port_Basic() => PortToTripleSlashAsync("Basic");

        [Fact]
        public Task Port_Generics() => PortToTripleSlashAsync("Generics");

        private static async Task PortToTripleSlashAsync(
            string testDataDir,
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
                CsProj = Path.GetFullPath(testData.ProjectFilePath),
                SkipInterfaceImplementations = skipInterfaceImplementations,
                BinLogPath = testData.BinLogPath,
            };

            c.IncludedAssemblies.Add(assemblyName);

            if (!string.IsNullOrEmpty(namespaceName))
            {
                c.IncludedNamespaces.Add(namespaceName);
            }

            c.DirsDocsXml.Add(testData.DocsDir);

            CancellationTokenSource cts = new();

            VSLoader.LoadVSInstance();
            c.Loader = new MSBuildLoader(c.BinLogPath);

            await c.Loader.LoadMainProjectAsync(c.CsProj, c.IsMono, cts.Token);

            ToTripleSlashPorter porter = new(c);
            porter.CollectFiles();
            await porter.MatchSymbolsAsync(throwOnSymbolsNotFound: true, cts.Token);
            await porter.PortAsync(throwOnSymbolsNotFound: true, cts.Token);

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

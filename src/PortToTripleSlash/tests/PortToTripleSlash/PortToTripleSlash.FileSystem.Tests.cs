// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ApiDocsSync.Tests;
using Xunit;
using Xunit.Abstractions;

namespace ApiDocsSync.PortToTripleSlash.Tests
{
    public class PortToTripleSlash_FileSystem_Tests : BasePortTests
    {
        public PortToTripleSlash_FileSystem_Tests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public Task Port_Basic() => PortToTripleSlashAsync("Basic");

        [Fact]
        public Task Port_Generics() => PortToTripleSlashAsync("Generics");

        private static async Task PortToTripleSlashAsync(
            string testDataDir,
            bool skipInterfaceImplementations = true,
            string assemblyName = FileTestData.TestAssembly,
            string namespaceName = FileTestData.TestNamespace)
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
                SkipRemarks = false
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

            await porter.MatchSymbolsAsync(c.Loader.MainProject.Compilation, isMSBuildProject: true, cts.Token);
            await porter.PortAsync(isMSBuildProject: true, cts.Token);

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

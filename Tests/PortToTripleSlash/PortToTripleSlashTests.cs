#nullable enable
using Microsoft.Build.Locator;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
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

        private static void PortToTripleSlash(
            string testDataDir,
            bool save = true,
            bool skipInterfaceImplementations = true,
            string assemblyName = TestData.TestAssembly,
            string namespaceName = TestData.TestNamespace,
            string typeName = TestData.TestType)
        {
            using TestDirectory tempDir = new();

            PortToTripleSlashTestData testData = new(
                tempDir,
                testDataDir,
                assemblyName: assemblyName,
                namespaceName: namespaceName,
                typeName: typeName);

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

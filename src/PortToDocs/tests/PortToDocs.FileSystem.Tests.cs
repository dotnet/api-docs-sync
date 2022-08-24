// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace ApiDocsSync.Libraries.Tests
{
    public class PortToDocs_FileSystem_Tests : BasePortTests
    {
        public PortToDocs_FileSystem_Tests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        // Verifies the basic case of porting all regular fields.
        public void Port_Basic()
        {
            PortToDocsWithFileSystem("Basic", new Configuration() { MarkdownRemarks = true, Save = true });
        }

        [Fact]
        public void Port_DontAddMissingRemarks()
        {
            PortToDocsWithFileSystem("DontAddMissingRemarks", new Configuration() { MarkdownRemarks = true, Save = true });
        }

        [Fact]
        // Verifies porting of APIs living in namespaces whose name match their assembly.
        public void Port_AssemblyAndNamespaceSame()
        {
            PortToDocsWithFileSystem("AssemblyAndNamespaceSame", new Configuration() { MarkdownRemarks = true, Save = true });
        }

        [Fact]
        // Verifies porting of APIs living in namespaces whose name does not match their assembly.
        public void Port_AssemblyAndNamespaceDifferent()
        {
            PortToDocsWithFileSystem("AssemblyAndNamespaceDifferent",
                       new Configuration() { MarkdownRemarks = true, Save = true },
                       namespaceNames: new[] { FileTestData.TestNamespace });
        }

        [Fact]
        // Ports Type remarks from IntelliSense xml.
        // Ports Method remarks from IntelliSense xml.
        // No interface strings should be ported.
        public void Port_Remarks_NoEII_NoInterfaceRemarks()
        {
            Configuration c = new Configuration()
            {
                MarkdownRemarks = true,
                SkipInterfaceImplementations = true,
                SkipInterfaceRemarks = true,
                Save = true
            };
            PortToDocsWithFileSystem("Remarks_NoEII_NoInterfaceRemarks", c);
        }

        [Fact]
        // Ports Type remarks from IntelliSense xml.
        // Ports Method remarks from IntelliSense xml.
        // Ports EII message and interface method remarks.
        public void Port_Remarks_WithEII_WithInterfaceRemarks()
        {
            Configuration c = new Configuration()
            {
                MarkdownRemarks = true,
                SkipInterfaceImplementations = false,
                SkipInterfaceRemarks = false,
                Save = true
            };
            PortToDocsWithFileSystem("Remarks_WithEII_WithInterfaceRemarks", c);
        }

        [Fact]
        // Ports Type remarks from IntelliSense xml.
        // Ports Method remarks from IntelliSense xml.
        // Ports EII message but no interface method remarks.
        public void Port_Remarks_WithEII_NoInterfaceRemarks()
        {
            Configuration c = new Configuration()
            {
                MarkdownRemarks = true,
                SkipInterfaceImplementations = false,
                SkipInterfaceRemarks = true,
                Save = true
            };
            PortToDocsWithFileSystem("Remarks_WithEII_NoInterfaceRemarks", c);
        }

        [Fact]
        // Verifies that new exceptions are ported.
        public void Port_Exceptions()
        {
            PortToDocsWithFileSystem("Exceptions", new Configuration() { MarkdownRemarks = true, Save = true });
        }

        [Fact]
        // Verifies that when an exception has already been ported, but went through
        // language review, does not get ported if its above the difference threshold.
        public void Port_Exception_ExistingCref()
        {
            Configuration c = new Configuration()
            {
                MarkdownRemarks = true,
                PortExceptionsExisting = true,
                ExceptionCollisionThreshold = 60,
                Save = true
            };
            PortToDocsWithFileSystem("Exception_ExistingCref", c);
        }

        [Fact]
        // Avoid porting enum field remarks
        public void Port_EnumRemarks()
        {
            PortToDocsWithFileSystem("EnumRemarks", new Configuration() { MarkdownRemarks = true, Save = true });
        }

        [Fact]
        // When the inheritdoc label is found, look for the parent type's documentation.
        // The parent type is located in a different assembly.
        public void Port_InheritDoc()
        {
            PortToDocsWithFileSystem("InheritDoc",
                       new Configuration() { MarkdownRemarks = true, Save = true },
                       assemblyNames: new[] { FileTestData.TestAssembly, "System" });
        }

        private static readonly string TestDataRootDir = Path.Join("..", "..", "..", "TestData");
        private static readonly string IntellisenseDir = "intellisense";
        private static readonly string XmlExpectedDir = "xml_expected";
        private static readonly string XmlActualDir = "xml";

        private static void DirectoryRecursiveCopy(string sourceDir, string targetDir)
        {
            DirectoryInfo dir = new(sourceDir);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException($"Source directory does not exist or could not be found: {sourceDir}");
            }

            Directory.CreateDirectory(targetDir);

            foreach (FileInfo file in dir.GetFiles())
            {
                string tempPath = Path.Combine(targetDir, file.Name);
                file.CopyTo(tempPath, false);
            }

            foreach (DirectoryInfo subdir in dir.GetDirectories())
            {
                string tempPath = Path.Combine(targetDir, subdir.Name);
                DirectoryRecursiveCopy(subdir.FullName, tempPath);
            }
        }

        private static void PortToDocsWithFileSystem(
                string testName,
                Configuration c,
                string[] assemblyNames = null,
                string[] namespaceNames = null) // Most namespaces have the same assembly name
        {
            assemblyNames ??= new string[] { FileTestData.TestAssembly };
            namespaceNames ??= Array.Empty<string>();

            using TestDirectory testDirectory = new();

            string targetDir = Path.Join(testDirectory.FullPath, testName);
            Directory.CreateDirectory(targetDir);

            string sourceDir = Path.Join(TestDataRootDir, testName);

            DirectoryRecursiveCopy(sourceDir, targetDir);

            foreach (string assemblyName in assemblyNames)
            {
                c.IncludedAssemblies.Add(assemblyName);
            }

            foreach (string namespaceName in namespaceNames)
            {
                c.IncludedNamespaces.Add(namespaceName);
            }

            c.DirsDocsXml.Add(new(Path.Join(targetDir, XmlActualDir)));
            c.DirsIntelliSense.Add(new(Path.Join(targetDir, IntellisenseDir)));

            var porter = new ToDocsPorter(c);
            porter.CollectFiles();
            porter.Start();
            porter.SaveToDisk();

            Verify(targetDir);
        }

        private static void Verify(string rootPath)
        {
            EnumerationOptions o = new() { RecurseSubdirectories = true };
            FileInfo[] expectedXmlFiles = new DirectoryInfo(Path.Join(rootPath, XmlExpectedDir)).GetFiles("*.xml", o);
            FileInfo[] actualXmlFiles = new DirectoryInfo(Path.Join(rootPath, XmlActualDir)).GetFiles("*.xml", o);

            foreach (var expectedFile in expectedXmlFiles)
            {
                FileInfo actualFile = actualXmlFiles.FirstOrDefault(x =>
                    x.Name == expectedFile.Name &&
                    Path.GetFileName(x.DirectoryName) == Path.GetFileName(expectedFile.DirectoryName));
                Assert.NotNull(actualFile);

                string expectedText = File.ReadAllText(expectedFile.FullName);
                string actualText = File.ReadAllText(actualFile.FullName);

                Assert.Equal(expectedText, actualText);
            }
        }
    }
}

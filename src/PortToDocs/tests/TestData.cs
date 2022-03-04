using System.IO;

namespace ApiDocsSync.Libraries.Tests
{
    internal class TestData
    {
        internal const string TestAssembly = "MyAssembly";
        internal const string TestNamespace = "MyNamespace";
        internal const string TestType = "MyType";
        internal const string DocsDirName = "Docs";

        internal string ExpectedFilePath { get; set; }
        internal string ActualFilePath { get; set; }
        internal DirectoryInfo DocsDir { get; set; }

    }
}

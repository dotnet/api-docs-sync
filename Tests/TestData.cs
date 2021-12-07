using System.IO;

namespace Libraries.Tests
{
    internal class TestData
    {
        internal const string TestAssembly = "MyAssembly";
        internal const string TestNamespace = "MyNamespace";
        internal const string TestType = "MyType";
        internal const string DocsDirName = "Docs";

        internal string ActualFilePath { get; set; }
        internal DirectoryInfo DocsDir { get; set; }

    }
}

using System.IO;

namespace Libraries.Tests
{
    internal class TestData
    {
        public const string TestAssembly = "MyAssembly";
        public const string TestNamespace = "MyNamespace";
        public const string TestType = "MyType";

        protected const string DocsDirName = "Docs";

        protected string Assembly { get; set; }
        protected string Namespace { get; set; }
        protected string Type { get; set; }

        internal DirectoryInfo DocsDir { get; set; }
        internal string OriginalFilePath { get; set; }
        internal string ExpectedFilePath { get; set; }
        internal string ActualFilePath { get; set; }
    }
}

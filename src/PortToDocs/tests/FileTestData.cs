// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace ApiDocsSync.Libraries.Tests
{
    internal class FileTestData
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

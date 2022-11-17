// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;

namespace ApiDocsSync.PortToTripleSlash.Tests;

internal class StringTestData
{
    public StringTestData(string docFile, string originalCode, string expectedCode)
    {
        DocFile = docFile;
        OriginalCode = originalCode;
        ExpectedCode = expectedCode;
        XDoc = XDocument.Parse(DocFile);
    }
    public string DocFile { get; }
    public string OriginalCode { get; }
    public string ExpectedCode { get; }
    public XDocument XDoc { get; }
}

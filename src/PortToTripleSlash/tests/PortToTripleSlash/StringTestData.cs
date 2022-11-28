// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Xml.Linq;

namespace ApiDocsSync.PortToTripleSlash.Tests;

internal class StringTestData
{
    public StringTestData(IEnumerable<string> docFiles, IEnumerable<string> originalCodeFiles, Dictionary<string, string> expectedCodeFiles, bool addMsCorLibReferences)
    {
        OriginalCodeFiles = originalCodeFiles;
        ExpectedCodeFiles = expectedCodeFiles;
        XDocs = new List<XDocument>();
        foreach (string docFile in docFiles)
        {
            XDocs.Add(XDocument.Parse(docFile));
        }
        AddMsCorLibReferences = addMsCorLibReferences;
    }
    public bool AddMsCorLibReferences { get; }
    public List<XDocument> XDocs { get; }
    public IEnumerable<string> OriginalCodeFiles { get; }
    public Dictionary<string, string> ExpectedCodeFiles { get; }
}

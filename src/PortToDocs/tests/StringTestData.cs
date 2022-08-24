// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;

namespace ApiDocsSync.Libraries.Tests
{
    internal class StringTestData
    {
        public StringTestData(string original, string expected)
        {
            Original = original;
            Expected = expected;
            XDoc = XDocument.Parse(original);
        }

        public string Original { get; }
        public string Expected { get; }
        public XDocument XDoc { get; }
        public string Actual => XDoc.ToString();
    }
}

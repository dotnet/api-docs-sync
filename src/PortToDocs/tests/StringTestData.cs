// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace ApiDocsSync.PortToDocs.Tests
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
        public string Actual
        {
            get
            {
                XmlWriterSettings xws = new()
                {
                    Encoding = Encoding.UTF8,
                    OmitXmlDeclaration = true,
                    Indent = true,
                    CheckCharacters = true,
                    NewLineChars = Configuration.NewLine,
                    NewLineHandling = NewLineHandling.Replace
                };
                using MemoryStream ms = new();
                using (XmlWriter xw = XmlWriter.Create(ms, xws))
                {
                    XDoc.Save(xw);
                }
                ms.Position = 0;
                using StreamReader sr = new(ms, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                return sr.ReadToEnd();
            }
        }
    }
}

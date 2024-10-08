// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;

namespace ApiDocsSync.PortToTripleSlash.Docs
{
    internal class DocsTypeSignature
    {
        private readonly XElement XETypeSignature;

        public string Language => XmlHelper.GetAttributeValue(XETypeSignature, "Language");

        public string Value => XmlHelper.GetAttributeValue(XETypeSignature, "Value");

        public DocsTypeSignature(XElement xeTypeSignature) => XETypeSignature = xeTypeSignature;
    }
}

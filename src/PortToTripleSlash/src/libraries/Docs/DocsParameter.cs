// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;

namespace ApiDocsSync.PortToTripleSlash.Docs
{
    internal class DocsParameter
    {
        private readonly XElement XEParameter;

        public string Name => XmlHelper.GetAttributeValue(XEParameter, "Name");

        public string Type => XmlHelper.GetAttributeValue(XEParameter, "Type");

        public DocsParameter(XElement xeParameter) => XEParameter = xeParameter;
    }
}

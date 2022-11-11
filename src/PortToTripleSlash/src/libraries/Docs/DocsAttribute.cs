// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;

namespace ApiDocsSync.PortToTripleSlash.Docs
{
    internal class DocsAttribute
    {
        private readonly XElement XEAttribute;

        public string FrameworkAlternate
        {
            get
            {
                return XmlHelper.GetAttributeValue(XEAttribute, "FrameworkAlternate");
            }
        }
        public string AttributeName
        {
            get
            {
                return XmlHelper.GetChildElementValue(XEAttribute, "AttributeName");
            }
        }

        public DocsAttribute(XElement xeAttribute)
        {
            XEAttribute = xeAttribute;
        }
    }
}

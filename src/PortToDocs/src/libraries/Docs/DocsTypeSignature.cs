// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;

namespace ApiDocsSync.Libraries.Docs
{
    internal class DocsTypeSignature
    {
        private readonly XElement XETypeSignature;

        public string Language
        {
            get
            {
                return XmlHelper.GetAttributeValue(XETypeSignature, "Language");
            }
        }

        public string Value
        {
            get
            {
                return XmlHelper.GetAttributeValue(XETypeSignature, "Value");
            }
        }

        public DocsTypeSignature(XElement xeTypeSignature)
        {
            XETypeSignature = xeTypeSignature;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;

namespace ApiDocsSync.Libraries.Docs
{
    internal class DocsParameter
    {
        private readonly XElement XEParameter;
        public string Name
        {
            get
            {
                return XmlHelper.GetAttributeValue(XEParameter, "Name");
            }
        }
        public string Type
        {
            get
            {
                return XmlHelper.GetAttributeValue(XEParameter, "Type");
            }
        }
        public DocsParameter(XElement xeParameter)
        {
            XEParameter = xeParameter;
        }
    }
}

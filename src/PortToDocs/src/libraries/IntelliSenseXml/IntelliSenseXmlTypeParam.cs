// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;

namespace ApiDocsSync.Libraries.IntelliSenseXml
{
    internal class IntelliSenseXmlTypeParam
    {
        public XElement XETypeParam;

        private string _name = string.Empty;
        public string Name
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_name))
                {
                    _name = XmlHelper.GetAttributeValue(XETypeParam, "name");
                }
                return _name;
            }
        }

        private string _value = string.Empty;
        public string Value
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_value))
                {
                    _value = XmlHelper.GetNodesInPlainText(XETypeParam);
                }
                return _value;
            }
        }

        public IntelliSenseXmlTypeParam(XElement xeTypeParam)
        {
            XETypeParam = xeTypeParam;
        }
    }
}

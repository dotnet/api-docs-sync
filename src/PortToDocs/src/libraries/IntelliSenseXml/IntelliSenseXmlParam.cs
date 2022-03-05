// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;

namespace ApiDocsSync.Libraries.IntelliSenseXml
{
    internal class IntelliSenseXmlParam
    {
        public XElement XEParam
        {
            get;
            private set;
        }

        private string _name = string.Empty;
        public string Name
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_name))
                {
                    _name = XmlHelper.GetAttributeValue(XEParam, "name");
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
                    _value = XmlHelper.GetNodesInPlainText(XEParam);
                }
                return _value;
            }
        }

        public IntelliSenseXmlParam(XElement xeParam)
        {
            XEParam = xeParam;
        }
    }
}

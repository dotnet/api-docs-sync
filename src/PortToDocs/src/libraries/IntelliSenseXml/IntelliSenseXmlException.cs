// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;

namespace ApiDocsSync.Libraries.IntelliSenseXml
{
    internal class IntelliSenseXmlException
    {
        public XElement XEException
        {
            get;
            private set;
        }

        private string _cref = string.Empty;
        public string Cref
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_cref))
                {
                    _cref = XmlHelper.GetAttributeValue(XEException, "cref");
                }
                return _cref;
            }
        }

        private string _value = string.Empty;
        public string Value
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_value))
                {
                    _value = XmlHelper.GetNodesInPlainText(XEException);
                }
                return _value;
            }
        }

        public IntelliSenseXmlException(XElement xeException)
        {
            XEException = xeException;
        }

        public override string ToString()
        {
            return $"{Cref} - {Value}";
        }
    }
}

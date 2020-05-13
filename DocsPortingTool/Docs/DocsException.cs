using System;
using System.Xml.Linq;

namespace DocsPortingTool.Docs
{
    public class DocsException
    {
        private readonly XElement XEException = null;

        public IDocsAPI ParentAPI
        {
            get; private set;
        }

        public string Cref
        {
            get
            {
                return XmlHelper.GetAttributeValue(XEException, "cref");
            }
        }

        public string Value
        {
            get
            {
                return XmlHelper.GetNodesInPlainText(XEException);
            }
            private set
            {
                XmlHelper.SaveFormattedAsXml(XEException, value);
            }
        }

        public string OriginalValue { get; private set; }

        public DocsException(IDocsAPI parentAPI, XElement xException)
        {
            ParentAPI = parentAPI;
            XEException = xException;
            OriginalValue = Value;
        }

        public void AppendException(string toAppend)
        {
            XmlHelper.AppendFormattedAsXml(XEException, "\r\n\r\n-or-\r\n\r\n" + toAppend);
            ParentAPI.Changed = true;
        }

        public override string ToString()
        {
            return $"{Cref} - {Value}";
        }
    }
}

using System;
using System.Xml.Linq;

namespace DocsPortingTool.Docs
{
    public class DocsException
    {
        private XElement XEException = null;

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
            XmlHelper.AppendFormattedAsXml(XEException,
                Environment.NewLine + Environment.NewLine + "-or-" + Environment.NewLine + Environment.NewLine + toAppend);
            ParentAPI.Changed = true;
        }

        public override string ToString()
        {
            return $"{Cref} - {Value}";
        }
    }
}

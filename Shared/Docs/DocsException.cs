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
                return XmlHelper.GetRealValue(XEException);
            }
            private set
            {
                XEException.Value = value;
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
            Value += Environment.NewLine + Environment.NewLine + "-or-" + Environment.NewLine + Environment.NewLine + toAppend;
            XmlHelper.FormatAsNormalElement(XEException);
        }

        public override string ToString()
        {
            return $"{Cref} - {Value}";
        }
    }
}

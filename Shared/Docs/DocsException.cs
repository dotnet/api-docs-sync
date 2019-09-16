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
            set
            {
                XmlHelper.FormatAsNormalElement(ParentAPI, XEException, value);
            }
        }

        public string OriginalValue { get; private set; }

        public DocsException(IDocsAPI parentAPI, string cref, string value)
        {
            ParentAPI = parentAPI;
            OriginalValue = value;
            XEException = new XElement("exception", string.Empty);
            XEException.SetAttributeValue("cref", cref);
            Value = value; // Ensure correct formatting
        }

        public DocsException(IDocsAPI parentAPI, XElement xException)
        {
            ParentAPI = parentAPI;
            XEException = xException;
            OriginalValue = Value;
        }

        public override string ToString()
        {
            return $"{Cref} - {Value}";
        }
    }
}

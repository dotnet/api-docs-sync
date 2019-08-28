using System.Xml.Linq;

namespace DocsPortingTool.Docs
{
    public class DocsException
    {
        private XDocument XDoc = null;
        private XElement XEException = null;
        private XElement Docs = null;

        public string FilePath { get; private set; }

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
                XmlHelper.SaveAsNonRemark(FilePath, XDoc, XEException, value);
            }
        }

        public string OriginalValue { get; private set; }

        public DocsException(string filePath, XDocument xDoc, XElement docs, string cref, string value)
        {
            FilePath = filePath;
            XDoc = xDoc;
            Docs = docs;
            OriginalValue = value;
            XElement xException = new XElement("exception", value);
            xException.SetAttributeValue("cref", cref);
            XEException = XmlHelper.SaveChildAsNonRemark(FilePath, XDoc, Docs, xException);
        }

        public DocsException(string filePath, XDocument xDoc, XElement docs, XElement xException)
        {
            FilePath = filePath;
            XDoc = xDoc;
            Docs = docs;
            XEException = xException;
            OriginalValue = Value;
        }

        public override string ToString()
        {
            return $"{Cref} - {Value}";
        }
    }
}

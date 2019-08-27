using System.Xml.Linq;

namespace DocsPortingTool.Docs
{
    public class DocsException
    {
        private XDocument XDoc = null;
        private XElement XEException = null;
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

        public DocsException(string filePath, XDocument xDoc, XElement xeException)
        {
            FilePath = filePath;
            XDoc = xDoc;
            XEException = xeException;
        }
    }
}

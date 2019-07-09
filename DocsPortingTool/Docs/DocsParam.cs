using System.Xml.Linq;

namespace DocsPortingTool.Docs
{
    public class DocsParam
    {
        private XDocument XDoc = null;
        private XElement XEDocsParam = null;
        public string FilePath { get; private set; }
        public string Name
        {
            get
            {
                return XmlHelper.GetAttributeValue(XEDocsParam, "name");
            }
        }
        public string Value
        {
            get
            {
                return XmlHelper.GetRealValue(XEDocsParam);
            }
            set
            {
                XmlHelper.SaveAsNonRemark(FilePath, XDoc, XEDocsParam, value);
            }
        }
        public DocsParam(string filePath, XDocument xDoc, XElement xeDocsParam)
        {
            FilePath = filePath;
            XDoc = xDoc;
            XEDocsParam = xeDocsParam;
        }
    }
}
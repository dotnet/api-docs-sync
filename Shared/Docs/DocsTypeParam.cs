using System.Xml.Linq;

namespace DocsPortingTool.Docs
{
    /// <summary>
    /// Each one of these typeparam objects live inside the Docs section inside the Member object.
    /// </summary>
    public class DocsTypeParam
    {
        private XDocument XDoc = null;
        private XElement XETypeParam = null;
        public string FilePath { get; private set; }
        public string Name
        {
            get
            {
                return XmlHelper.GetAttributeValue(XETypeParam, "name");
            }
        }
        public string Value
        {
            get
            {
                return XmlHelper.GetRealValue(XETypeParam);
            }
            set
            {
                XmlHelper.SaveAsNonRemark(FilePath, XDoc, XETypeParam, value);
            }
        }

        public DocsTypeParam(string filePath, XDocument xDoc, XElement xeTypeParam)
        {
            FilePath = filePath;
            XDoc = xDoc;
            XETypeParam = xeTypeParam;
        }
    }
}
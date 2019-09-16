using System.Xml.Linq;

namespace DocsPortingTool.Docs
{
    public class DocsParam
    {
        private XElement XEDocsParam = null;
        public IDocsAPI ParentAPI
        {
            get; private set;
        }
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
                XmlHelper.FormatAsNormalElement(ParentAPI, XEDocsParam, value);
            }
        }
        public DocsParam(IDocsAPI parentAPI, XElement xeDocsParam)
        {
            ParentAPI = parentAPI;
            XEDocsParam = xeDocsParam;
        }
        public override string ToString()
        {
            return Name;
        }
    }
}
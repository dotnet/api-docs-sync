using System.Xml.Linq;

namespace Libraries.Docs
{
    public class DocsParam : DocsTextElement
    {
        private readonly XElement XEDocsParam;
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
                return XmlHelper.GetNodesInPlainText(XEDocsParam);
            }
            set
            {
                XmlHelper.SaveFormattedAsXml(XEDocsParam, value);
                ParentAPI.Changed = true;
            }
        }
        public DocsParam(IDocsAPI parentAPI, XElement xeDocsParam) : base(xeDocsParam)
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
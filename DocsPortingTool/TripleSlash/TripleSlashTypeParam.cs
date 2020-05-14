using System.Xml.Linq;

namespace DocsPortingTool.TripleSlash
{
    public class TripleSlashTypeParam
    {
        public XElement XETypeParam;

        private string _name = string.Empty;
        public string Name
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_name))
                {
                    _name = XmlHelper.GetAttributeValue(XETypeParam, "name");
                }
                return _name;
            }
        }

        private string _value = string.Empty;
        public string Value
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_value))
                {
                    _value = XmlHelper.GetNodesInPlainText(XETypeParam);
                }
                return _value;
            }
        }

        public TripleSlashTypeParam(XElement xeTypeParam)
        {
            XETypeParam = xeTypeParam;
        }
    }
}
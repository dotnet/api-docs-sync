using System.Xml.Linq;

namespace DocsPortingTool.TripleSlash
{
    public class TripleSlashParam
    {
        public XElement XEParam
        {
            get;
            private set;
        }

        private string _name = string.Empty;
        public string Name
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_name))
                {
                    _name = XmlHelper.GetAttributeValue(XEParam, "name");
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
                    _value = XmlHelper.GetRealValue(XEParam);
                }
                return _value;
            }
        }

        public TripleSlashParam(XElement xeParam)
        {
            XEParam = xeParam;
        }
    }
}

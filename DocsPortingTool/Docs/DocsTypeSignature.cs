using System.Xml.Linq;

namespace DocsPortingTool.Docs
{
    public class DocsTypeSignature
    {
        private readonly XElement XETypeSignature = null;

        public string Language
        {
            get
            {
                return XmlHelper.GetAttributeValue(XETypeSignature, "Language");
            }
        }

        public string Value
        {
            get
            {
                return XmlHelper.GetAttributeValue(XETypeSignature, "Value");
            }
        }

        public DocsTypeSignature(XElement xeTypeSignature)
        {
            XETypeSignature = xeTypeSignature;
        }
    }
}
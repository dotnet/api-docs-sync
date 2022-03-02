using System.Xml.Linq;

namespace DocsPortingTool.Libraries.Docs
{
    internal class DocsTypeSignature
    {
        private readonly XElement XETypeSignature;

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
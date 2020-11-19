#nullable enable
using System.Xml.Linq;

namespace Libraries.Docs
{
    internal class DocsSeeAlso
    {
        private readonly XElement XESeeAlso;

        public IDocsAPI ParentAPI
        {
            get; private set;
        }

        public string Cref
        {
            get
            {
                return XmlHelper.GetAttributeValue(XESeeAlso, "cref");
            }
        }

        public DocsSeeAlso(IDocsAPI parentAPI, XElement xeSeeAlso)
        {
            ParentAPI = parentAPI;
            XESeeAlso = xeSeeAlso;
        }

        public override string ToString()
        {
            return $"{Cref}";
        }
    }
}

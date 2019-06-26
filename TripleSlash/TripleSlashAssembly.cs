using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace DocsPortingTool.TripleSlash
{
    public class TripleSlashAssembly
    {
        private XElement XERoot = null;
        public string FilePath { get; private set; }

        private string _assembly = string.Empty;
        public string Name
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_assembly))
                {
                    XElement xeAssembly = XmlHelper.GetChildElement(XERoot, "assembly");
                    if (xeAssembly != null)
                    {
                        _assembly = XmlHelper.GetChildElementValue(xeAssembly, "name");
                    }
                }

                return _assembly;
            }
        }

        private List<TripleSlashMember> _members;
        public List<TripleSlashMember> Members
        {
            get
            {
                if (_members == null)
                {
                    XElement xeMembers = XmlHelper.GetChildElement(XERoot, "members");
                    if (xeMembers != null)
                    {
                        _members = xeMembers.Elements("member").Select(x => new TripleSlashMember(x)).ToList();
                    }
                }
                return _members;
            }
        }

        public TripleSlashAssembly(string filePath, XElement xeRoot)
        {
            FilePath = filePath;
            XERoot = xeRoot;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}

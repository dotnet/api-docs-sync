using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace DocsPortingTool.TripleSlash
{
    public class TripleSlashMember
    {
        private XElement XEMember = null;

        private string _name = string.Empty;
        public string Name
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_name))
                {
                    _name = XmlHelper.GetAttributeValue(XEMember, "name");
                }
                return _name;
            }
        }

        private List<TripleSlashParam> _params;
        public List<TripleSlashParam> Params
        {
            get
            {
                if (_params == null)
                {
                    _params = XEMember.Elements("param").Select(x => new TripleSlashParam(x)).ToList();
                }
                return _params;
            }
        }

        private List<TripleSlashTypeParam> _typeParams;
        public List<TripleSlashTypeParam> TypeParams
        {
            get
            {
                if (_typeParams == null)
                {
                    _typeParams = XEMember.Elements("typeparam").Select(x => new TripleSlashTypeParam(x)).ToList();
                }
                return _typeParams;
            }
        }

        private List<TripleSlashException> _exceptions;
        public IEnumerable<TripleSlashException> Exceptions
        {
            get
            {
                if (_exceptions == null)
                {
                    _exceptions = XEMember.Elements("exception").Select(x => new TripleSlashException(x)).ToList();
                }
                return _exceptions;
            }
        }

        private string _summary = string.Empty;
        public string Summary
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_summary))
                {
                    _summary = XmlHelper.GetChildElementValue(XEMember, "summary");
                }
                return _summary;
            }
        }

        private string _returns = string.Empty;
        public string Returns
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_returns))
                {
                    _returns = XmlHelper.GetChildElementValue(XEMember, "returns");
                }
                return _returns;
            }
        }

        private string _remarks = string.Empty;
        public string Remarks
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_remarks))
                {
                    _remarks = XmlHelper.GetChildElementValue(XEMember, "remarks");
                }
                return _remarks;
            }
        }

        public TripleSlashMember(XElement xeMember)
        {
            XEMember = xeMember;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}

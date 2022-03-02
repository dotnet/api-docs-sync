using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace DocsPortingTool.Libraries.IntelliSenseXml
{
    internal class IntelliSenseXmlMember
    {
        private readonly XElement XEMember;

        private XElement? _xInheritDoc = null;
        private XElement? XInheritDoc
        {
            get
            {
                if (_xInheritDoc == null)
                {
                    _xInheritDoc = XEMember.Elements("inheritdoc").FirstOrDefault();
                }
                return _xInheritDoc;
            }
        }

        public string Assembly { get; private set; }

        private string? _inheritDocCref = null;
        public string InheritDocCref
        {
            get
            {
                if (_inheritDocCref == null)
                {
                    _inheritDocCref = string.Empty;
                    if (InheritDoc && XInheritDoc != null)
                    {
                        XAttribute? xInheritDocCref = XInheritDoc.Attribute("cref");
                        if (xInheritDocCref != null)
                        {
                            _inheritDocCref = xInheritDocCref.Value.DocIdEscaped();
                        }
                    }
                }
                return _inheritDocCref;
            }
        }

        private bool? _inheritDoc = null;
        public bool InheritDoc
        {
            get
            {
                if (!_inheritDoc.HasValue)
                {
                    _inheritDoc = XInheritDoc != null;

                }
                return _inheritDoc.Value;
            }
        }

        private string _namespace = string.Empty;
        public string Namespace
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_namespace))
                {
                    string[] splittedParenthesis = Name.Split('(', StringSplitOptions.RemoveEmptyEntries);
                    string withoutParenthesisAndPrefix = splittedParenthesis[0][2..]; // Exclude the "X:" prefix
                    string[] splittedDots = withoutParenthesisAndPrefix.Split('.', StringSplitOptions.RemoveEmptyEntries);

                    _namespace = string.Join('.', splittedDots.Take(splittedDots.Length - 1));
                }

                return _namespace;
            }
        }

        private string? _name;
        /// <summary>
        /// The API DocId.
        /// </summary>
        public string Name
        {
            get
            {
                if (_name == null)
                {
                    // The member name is a DocId
                    _name = XmlHelper.GetAttributeValue(XEMember, "name").DocIdEscaped();
                }
                return _name;
            }
        }

        private List<IntelliSenseXmlParam>? _params;
        public List<IntelliSenseXmlParam> Params
        {
            get
            {
                if (_params == null)
                {
                    _params = XEMember.Elements("param").Select(x => new IntelliSenseXmlParam(x)).ToList();
                }
                return _params;
            }
        }

        private List<IntelliSenseXmlTypeParam>? _typeParams;
        public List<IntelliSenseXmlTypeParam> TypeParams
        {
            get
            {
                if (_typeParams == null)
                {
                    _typeParams = XEMember.Elements("typeparam").Select(x => new IntelliSenseXmlTypeParam(x)).ToList();
                }
                return _typeParams;
            }
        }

        private List<IntelliSenseXmlException>? _exceptions;
        public IEnumerable<IntelliSenseXmlException> Exceptions
        {
            get
            {
                if (_exceptions == null)
                {
                    _exceptions = XEMember.Elements("exception").Select(x => new IntelliSenseXmlException(x)).ToList();
                }
                return _exceptions;
            }
        }

        private string? _summary;
        public string Summary
        {
            get
            {
                if (_summary == null)
                {
                    XElement? xElement = XEMember.Element("summary");
                    _summary = (xElement != null) ? XmlHelper.GetNodesInPlainText(xElement) : string.Empty;
                }
                return _summary;
            }
        }

        public string? _value;
        public string Value
        {
            get
            {
                if (_value == null)
                {
                    XElement? xElement = XEMember.Element("value");
                    _value = (xElement != null) ? XmlHelper.GetNodesInPlainText(xElement) : string.Empty;
                }
                return _value;
            }
        }

        private string? _returns;
        public string Returns
        {
            get
            {
                if (_returns == null)
                {
                        XElement? xElement = XEMember.Element("returns");
                        _returns = (xElement != null) ? XmlHelper.GetNodesInPlainText(xElement) : string.Empty;
                }
                return _returns;
            }
        }

        private string? _remarks;
        public string Remarks
        {
            get
            {
                if (_remarks == null)
                {
                        XElement? xElement = XEMember.Element("remarks");
                        _remarks = (xElement !=  null) ? XmlHelper.GetNodesInPlainText(xElement) : string.Empty;
                }
                return _remarks;
            }
        }

        public IntelliSenseXmlMember(XElement xeMember, string assembly)
        {
            if (string.IsNullOrEmpty(assembly))
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            XEMember = xeMember ?? throw new ArgumentNullException(nameof(xeMember));
            Assembly = assembly.Trim();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}

using System.Collections.Generic;
using System.Xml.Linq;

namespace DocsPortingTool.Docs
{
    public interface IDocsParamWrapper
    {
        public abstract string FilePath { get; set; }
        public abstract string DocId { get; }
        public abstract XElement Docs { get; }
        public abstract List<DocsParameter> Parameters { get; }
        public abstract List<DocsParam> Params { get; }
        public abstract DocsParam SaveParam(XElement xeCoreFXParam);
    }

    public abstract class DocsParamWrapper : IDocsParamWrapper
    {
        protected XDocument XDoc = null;
        public string FilePath { get; set; }
        public abstract string DocId { get; }
        public abstract XElement Docs { get; }
        public abstract List<DocsParameter> Parameters { get; }
        public abstract List<DocsParam> Params { get; }
        public DocsParam SaveParam(XElement xeCoreFXParam)
        {
            XElement xeDocsParam = XmlHelper.SaveChildAsNonRemark(FilePath, XDoc, Docs, xeCoreFXParam);
            DocsParam docsParam = new DocsParam(FilePath, XDoc, xeDocsParam);
            return docsParam;
        }
    }
}

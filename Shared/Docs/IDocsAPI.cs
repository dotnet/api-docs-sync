using System.Collections.Generic;
using System.Xml.Linq;

namespace DocsPortingTool.Docs
{
    public interface IDocsAPI
    {
        public abstract string Prefix { get; }
        public abstract bool Changed { get; set; }
        public abstract string FilePath { get; set; }
        public abstract string DocId { get; }
        public abstract XElement Docs { get; }
        public abstract List<DocsParameter> Parameters { get; }
        public abstract List<DocsParam> Params { get; }
        public abstract string Summary { get; set; }
        public abstract string Remarks { get; set; }
        public abstract DocsParam SaveParam(XElement xeCoreFXParam);
    }
}

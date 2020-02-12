using Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace DocsPortingTool.Docs
{
    public interface IDocsAPI
    {
        public abstract XDocument XDoc { get; set; }
        public abstract bool Changed { get; set; }
        public abstract string FilePath { get; set; }
        public abstract Encoding OriginalEncoding { get; set; }
        public abstract string DocId { get; }
        public abstract XElement Docs { get; }
        public abstract List<DocsParameter> Parameters { get; }
        public abstract List<DocsParam> Params { get; }
        public abstract DocsParam SaveParam(XElement xeCoreFXParam);
        void AddChildAsNormalElement(XElement xeParent, XElement xeChild, bool errorCheck = false);
        void FormatAsNormalElement(XElement xeChild);
    }

    public abstract class DocsAPI : IDocsAPI
    {
        public XDocument XDoc { get; set; } = null;
        public bool Changed { get; set; } = false;
        public string FilePath { get; set; } = string.Empty;
        public Encoding OriginalEncoding { get; set; } = null;
        public abstract string DocId { get; }
        public abstract XElement Docs { get; }
        public abstract List<DocsParameter> Parameters { get; }
        public abstract List<DocsParam> Params { get; }

        public DocsParam SaveParam(XElement xeTripleSlashParam)
        {
            XElement xeDocsParam = new XElement(xeTripleSlashParam.Name, xeTripleSlashParam.Value);
            xeDocsParam.ReplaceAttributes(xeTripleSlashParam.Attributes());
            AddChildAsNormalElement(Docs, xeDocsParam, true);
            DocsParam docsParam = new DocsParam(this, xeDocsParam);
            return docsParam;
        }

        public void AddChildAsNormalElement(XElement xeParent, XElement xeChild, bool errorCheck = false)
        {
            if (VerifySaveChildParams(XDoc, xeParent, xeChild, true))
            {
                FormatAsNormalElement(xeChild);
                xeParent.Add(xeChild);
            }
        }

        public void FormatAsNormalElement(XElement xeChild)
        {
            XmlHelper.FormatAsNormalElement(xeChild);
            Changed = true;
        }

        private static bool VerifySaveChildParams(XDocument doc, XElement parent, XElement child, bool errorCheck = false)
        {
            if (doc == null)
            {
                if (errorCheck)
                {
                    Log.Error("A null XDocument was passed when attempting to save a new child element");
                }
                return false;
            }
            else if (parent == null)
            {
                if (errorCheck)
                {
                    Log.Error("A null XElement parent was passed when attempting to save a new child element");
                }
                return false;
            }
            else if (child == null)
            {
                if (errorCheck)
                {
                    Log.Error("A null XElement child was passed when attempting to save a new child element");
                }
                return false;
            }

            return true;
        }
    }
}

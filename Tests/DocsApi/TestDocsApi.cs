using System.Xml.Linq;

namespace Libraries.Docs.Tests
{
    class TestDocsApi : DocsAPI
    {
        public TestDocsApi() : base(XElement.Parse("<Type />")) { }

        public override bool Changed { get; set; }

        public override string DocId => "--DocId--";

        public override string Summary { get; set; }
        public override string Remarks { get; set; }
        public override string ReturnType { get; }
        public override string Returns { get; set; }
    }
}

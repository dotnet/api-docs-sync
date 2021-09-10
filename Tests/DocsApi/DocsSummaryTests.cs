using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Xunit;

namespace Libraries.Docs.Tests
{
    public class DocsSummaryTests
    {
        [Theory]
        [InlineData(
            @"<summary>Summary.</summary>",
            @"Summary.")]
        [InlineData(
            @"<summary>Summary referencing <see cref=""T:System.Int32"" />.</summary>",
            @"Summary referencing <see cref=""T:System.Int32"" />.")]
        [InlineData(
            @"<summary>
                Multiline
                Summary
                Referencing
                <see cref=""T:System.Int32"" />.
            </summary>",
            @"
                Multiline
                Summary
                Referencing
                <see cref=""T:System.Int32"" />.
            ")]
        public void GetsRawText(string xml, string expected)
        {
            var summary = new DocsSummary(XElement.Parse(xml));
            Assert.Equal(expected, summary.RawText);
        }

        [Theory]
        [InlineData(
            @"<summary>Summary.</summary>",
            @"Summary.")]
        [InlineData(
            @"<summary>Summary referencing <see cref=""T:System.Int32"" />.</summary>",
            @"Summary referencing <see cref=""int"" />.")]
        [InlineData(
            @"<summary>
                Multiline

                Summary

                Referencing

                <see cref=""T:System.Int32"" />

                With Blank Lines.
            </summary>",
            @"Multiline
Summary
Referencing
<see cref=""int"" />
With Blank Lines.")]
        public void GetsParsedText(string xml, string expected)
        {
            var summary = new DocsSummary(XElement.Parse(xml));
            Assert.Equal(expected, summary.ParsedText);
        }

        [Fact]
        public void AllowsInlineIncludesInParsedText()
        {
            var xml = @"<summary>Converts narrow (single-byte) characters in the string to wide (double-byte) characters. Applies to Asian locales. This member is equivalent to the Visual Basic constant <see langword=""vbWide"" />. [!INCLUDE[vbstrconv-wide](~/includes/vbstrconv-wide-md.md)]</summary>";
            var expected = @"Converts narrow (single-byte) characters in the string to wide (double-byte) characters. Applies to Asian locales. This member is equivalent to the Visual Basic constant <see langword=""vbWide"" />. [!INCLUDE[vbstrconv-wide](~/includes/vbstrconv-wide-md.md)]";

            var summary = new DocsSummary(XElement.Parse(xml));
            Assert.Equal(expected, summary.ParsedText);
        }

        [Fact]
        public void GetsNodes()
        {
            var xml = @"<summary>Summary referencing a <see cref=""T:System.Type"" />.</summary>";
            var summary = new DocsSummary(XElement.Parse(xml));

            var expected = new XNode[]
            {
                new XText("Summary referencing a "),
                XElement.Parse(@"<see cref=""T:System.Type"" />"),
                new XText(".")
            };

            Assert.Equal(expected.Select(x => x.ToString()), summary.RawNodes.ToArray().Select(x => x.ToString()));
        }

        [Fact]
        public void CanIncludeSeeElements()
        {
            var xml = @"<summary><see cref=""T:System.Type"" /></summary>";
            var summary = new DocsSummary(XElement.Parse(xml));
            var see = summary.RawNodes.Single();

            Assert.Equal(XmlNodeType.Element, see.NodeType);
        }

        [Fact]
        public void CanExposeRawSeeElements()
        {
            var xml = @"<summary><see cref=""T:System.Type"" /></summary>";
            var summary = new DocsSummary(XElement.Parse(xml));
            var see = summary.RawNodes.Single();

            Assert.Equal("see", ((XElement)see).Name);
        }

        [Fact]
        public void CanExposeRawSeeCrefValues()
        {
            var xml = @"<summary><see cref=""T:System.Type"" /></summary>";
            var summary = new DocsSummary(XElement.Parse(xml));
            var see = summary.RawNodes.Single();

            Assert.Equal("T:System.Type", ((XElement)see).Attribute("cref").Value);
        }

        [Fact]
        public void ParsesNodes()
        {
            var xml = @"<summary>Summary referencing a <see cref=""T:System.Collections.Generic.IEnumerable`1"" />.</summary>";
            var summary = new DocsSummary(XElement.Parse(xml));

            var expected = new XNode[]
            {
                new XText("Summary referencing a "),
                XElement.Parse(@"<see cref=""System.Collections.Generic.IEnumerable{T}"" />"),
                new XText(".")
            };

            Assert.Equal(expected.Select(x => x.ToString()), summary.ParsedNodes.ToArray().Select(x => x.ToString()));
        }
    }
}

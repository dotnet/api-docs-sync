using System.Xml.Linq;
using Xunit;

namespace Libraries.Docs.Tests
{
    public class DocsParamTests
    {
        [Theory]
        [InlineData(
            @"<param name=""image"">The <see cref=""T:System.Drawing.Image"" /> object to animate.</param>",
            @"image")]
        public void ExtractsName(string xml, string expected)
        {
            var parent = new TestDocsApi();
            var param = new DocsParam(parent, XElement.Parse(xml));

            Assert.Equal(expected, param.Name);
        }

        [Theory]
        [InlineData(
            @"<param name=""image"">The <see cref=""T:System.Drawing.Image"" /> object to animate.</param>",
            @"The <see cref=""T:System.Drawing.Image"" /> object to animate.")]
        [InlineData(
            @"<param name=""item"">The object to be added to the end of the <see cref=""T:System.Collections.Generic.List`1"" />. The value can be <see langword=""null"" /> for reference types.</param>",
            @"The object to be added to the end of the <see cref=""T:System.Collections.Generic.List`1"" />. The value can be <see langword=""null"" /> for reference types.")]
        public void ExtractsValueAsPlainText(string xml, string expected)
        {
            var parent = new TestDocsApi();
            var param = new DocsParam(parent, XElement.Parse(xml));

            Assert.Equal(expected, param.Value);
        }
    }
}

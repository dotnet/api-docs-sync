using System.Xml.Linq;
using Xunit;

namespace Libraries.Docs.Tests
{
    public class DocsParameterTests
    {
        [Theory]
        [InlineData(
            @"<Parameter Name=""image"" Type=""System.Drawing.Image"" />",
            @"image")]
        public void ExtractsName(string xml, string expected)
        {
            var parameter = new DocsParameter(XElement.Parse(xml));
            Assert.Equal(expected, parameter.Name);
        }

        [Theory]
        [InlineData(
            @"<Parameter Name=""image"" Type=""System.Drawing.Image"" />",
            @"System.Drawing.Image")]
        [InlineData(
            @"<Parameter Name=""collection"" Type=""System.Collections.Generic.IEnumerable&lt;T&gt;"" />",
            @"System.Collections.Generic.IEnumerable<T>")]
        public void ExtractsType(string xml, string expected)
        {
            var parameter = new DocsParameter(XElement.Parse(xml));
            Assert.Equal(expected, parameter.Type);
        }
    }
}

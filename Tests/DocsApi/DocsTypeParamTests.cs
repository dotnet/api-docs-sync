using System.Xml.Linq;
using Xunit;

namespace Libraries.Docs.Tests
{
    public class DocsTypeParamTests
    {
        [Theory]
        [InlineData(
            @"<typeparam name=""TKey"">The type of the keys in the dictionary.</typeparam>",
            @"TKey")]
        public void ExtractsName(string xml, string expected)
        {
            var parent = new TestDocsApi();
            var typeParam = new DocsTypeParam(parent, XElement.Parse(xml));

            Assert.Equal(expected, typeParam.Name);
        }

        [Theory]
        [InlineData(
            @"<typeparam name=""TKey"">The type of the keys in the dictionary.</typeparam>",
            @"The type of the keys in the dictionary.")]
        [InlineData(
            @"<typeparam name=""T"">The type of items in the <see cref=""T:System.ReadOnlySpan`1"" />.</typeparam>",
            @"The type of items in the <see cref=""T:System.ReadOnlySpan`1"" />.")]
        public void ExtractsValueAsPlainText(string xml, string expected)
        {
            var parent = new TestDocsApi();
            var typeParam = new DocsTypeParam(parent, XElement.Parse(xml));

            Assert.Equal(expected, typeParam.Value);
        }
    }
}

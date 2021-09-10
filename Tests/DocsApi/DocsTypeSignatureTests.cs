using System.Xml.Linq;
using Xunit;

namespace Libraries.Docs.Tests
{
    public class DocsTypeSignatureTests
    {
        [Theory]
        [InlineData(
            @"<TypeSignature Language=""C#"" Value=""public sealed class RequiredAttribute : Attribute"" />",
            @"C#")]
        [InlineData(
            @"<TypeSignature Language=""VB.NET"" Value=""Public NotInheritable Class RequiredAttribute &#xA;Inherits Attribute"" />",
            @"VB.NET")]
        public void ExtractsLanguage(string xml, string expected)
        {
            var typeSignature = new DocsTypeSignature(XElement.Parse(xml));
            Assert.Equal(expected, typeSignature.Language);
        }

        [Theory]
        [InlineData(
            @"<TypeSignature Language=""C#"" Value=""public sealed class RequiredAttribute : Attribute"" />",
            @"public sealed class RequiredAttribute : Attribute")]
        [InlineData(
            @"<TypeSignature Language=""VB.NET"" Value=""Public NotInheritable Class RequiredAttribute &#xA;Inherits Attribute"" />",
            "Public NotInheritable Class RequiredAttribute \nInherits Attribute")]
        public void ExtractsValue(string xml, string expected)
        {
            var typeSignature = new DocsTypeSignature(XElement.Parse(xml));
            Assert.Equal(expected, typeSignature.Value);
        }
    }
}

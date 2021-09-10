using System.Collections.Generic;
using System.Xml.Linq;
using Xunit;

namespace Libraries.Docs.Tests
{
    public class DocsTypeParameterTests
    {
        [Theory]
        [InlineData(
            @"<TypeParameter Name=""T"" />",
            @"T")]
        [InlineData(@"
            <TypeParameter Name=""T"">
              <Constraints>
                <ParameterAttribute>Covariant</ParameterAttribute>
              </Constraints>
            </TypeParameter>",
            @"T")]
        public void ExtractsName(string xml, string expected)
        {
            var typeParameter = new DocsTypeParameter(XElement.Parse(xml));
            Assert.Equal(expected, typeParameter.Name);
        }

        [Theory]
        [InlineData(
            @"<TypeParameter Name=""T"" />",
            new string[] { })]
        [InlineData(@"
            <TypeParameter Name=""T"">
              <Constraints>
                <ParameterAttribute>Covariant</ParameterAttribute>
              </Constraints>
            </TypeParameter>",
            new string[] { "Covariant" })]
        [InlineData(@"
            <TypeParameter Name=""T"">
              <Constraints>
                <ParameterAttribute>DefaultConstructorConstraint</ParameterAttribute>
                <ParameterAttribute>NotNullableValueTypeConstraint</ParameterAttribute>
                <BaseTypeName>System.ValueType</BaseTypeName>
              </Constraints>
            </TypeParameter>",
            new string[] { "DefaultConstructorConstraint", "NotNullableValueTypeConstraint" })]
        public void ExtractsConstraintsParameterAttributesAsPlainText(string xml, IEnumerable<string> expected)
        {
            var typeParameter = new DocsTypeParameter(XElement.Parse(xml));
            Assert.Equal(expected, typeParameter.ConstraintsParameterAttributes);
        }

        [Theory]
        [InlineData(@"
            <TypeParameter Name=""T"">
              <Constraints>
                <BaseTypeName>System.Data.DataRow</BaseTypeName>
              </Constraints>
            </TypeParameter>",
           @"System.Data.DataRow")]
        public void ExtractsConstraintsBaseTypeName(string xml, string expected)
        {
            var typeParameter = new DocsTypeParameter(XElement.Parse(xml));
            Assert.Equal(expected, typeParameter.ConstraintsBaseTypeName);
        }

        [Theory]
        [InlineData(@"
            <TypeParameter Name=""TComparable"">
              <Constraints>
                <InterfaceName>System.IComparable&lt;T&gt;</InterfaceName>
              </Constraints>
            </TypeParameter>",
           @"System.IComparable&lt;T&gt;")]
        public void ExtractsConstraintsInterfaceName(string xml, string expected)
        {
            var typeParameter = new DocsTypeParameter(XElement.Parse(xml));
            Assert.Equal(expected, typeParameter.ConstraintsInterfaceName);
        }
    }
}

using System.Xml.Linq;
using Xunit;

namespace Libraries.Docs.Tests
{
    public class DocsAssemblyInfoTests
    {
        [Fact]
        public void ExtractsAssemblyName()
        {
            var assembly = new DocsAssemblyInfo(XElement.Parse(@"
                <AssemblyInfo>
                    <AssemblyName>MyAssembly</AssemblyName>
                </AssemblyInfo>"
            ));

            Assert.Equal("MyAssembly", assembly.AssemblyName);
        }

        [Theory]
        [InlineData(@"
            <AssemblyInfo>
                <AssemblyVersion>4.0.0.0</AssemblyVersion>
            </AssemblyInfo>",
            new string[] { "4.0.0.0" })]
        [InlineData(@"
            <AssemblyInfo>
                <AssemblyVersion>4.0.0.0</AssemblyVersion>
                <AssemblyVersion>5.0.0.0</AssemblyVersion>
            </AssemblyInfo>",
            new string[] { "4.0.0.0", "5.0.0.0" })]
        public void ExtractsOneAssemblyVersion(string xml, string[] expected)
        {
            var assembly = new DocsAssemblyInfo(XElement.Parse(xml));

            Assert.Equal(expected, assembly.AssemblyVersions);
        }
    }
}

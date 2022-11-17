// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using ApiDocsSync.Tests;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using Xunit.Abstractions;

namespace ApiDocsSync.PortToTripleSlash.Tests;

public class PortToTripleSlash_Strings_Tests : BasePortTests
{
    public PortToTripleSlash_Strings_Tests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public Task EnumAsync()
    {
        string docId = "MyNamespace.MyEnum";

        string docFile = @"<Type Name=""MyEnum"" FullName=""MyNamespace.MyEnum"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyEnum"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>This is the MyEnum summary.</summary>
    <remarks>These are the MyEnum remarks.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""Value1"">
      <MemberSignature Language=""DocId"" Value=""F:MyNamespace.MyEnum.Value1"" />
      <Docs>
        <summary>This is the MyEnum.Value1 summary.</summary>
      </Docs>
    </Member>
    <Member MemberName=""Value2"">
      <MemberSignature Language=""DocId"" Value=""F:MyNamespace.MyEnum.Value2"" />
      <Docs>
        <summary>This is the MyEnum.Value2 summary.</summary>
      </Docs>
    </Member>
  </Members>
</Type>";
            string originalCode = @"public namespace MyNamespace
{
  public enum MyEnum
  {
    Value1,
    Value2
  }
}";
        string expectedCode = @"public namespace MyNamespace
{
  /// <summary>This is the MyEnum summary.</summary>
  /// <remarks>These are the MyEnum remarks.</remarks>
  public enum MyEnum
  {
    /// <summary>This is the MyEnum.Value1 summary.</summary>
    Value1,
    /// <summary>This is the MyEnum.Value2 summary.</summary>
    Value2
  }
}";

        Dictionary<string, StringTestData> data = new()
        {
            { docId, new StringTestData(docFile, originalCode, expectedCode) }
        };

        return TestWithStringsAsync(data);
    }

    private static Task TestWithStringsAsync(Dictionary<string, StringTestData> data)
    {
        Configuration c = new()
        {
            SkipInterfaceImplementations = false,
        };

        return TestWithStringsAsync(c, "MyAssembly", data);
    }

    private static async Task TestWithStringsAsync(Configuration c, string assembly, Dictionary<string, StringTestData> data)
    {
        c.IncludedAssemblies.Add(assembly);

        CancellationTokenSource cts = new();

        CSharpParseOptions parseOptions = new CSharpParseOptions().WithKind(SourceCodeKind.Regular);
        CSharpCompilationOptions compileOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithOptimizationLevel(OptimizationLevel.Release).WithAllowUnsafe(enabled: true);

        SyntaxTree tree = CSharpSyntaxTree.ParseText(data.First().Value.OriginalCode, parseOptions);

        CSharpCompilation compilation = CSharpCompilation.Create(assembly).WithOptions(compileOptions).AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location)).AddSyntaxTrees(tree); // reference same mscorlib we're running on

        ToTripleSlashPorter porter = new(c);

        UTF8Encoding utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

        int fileNumber = 0;
        foreach ((string testDocId, StringTestData testElement) in data)
        {
            porter.LoadDocsFile(testElement.XDoc, $"File{fileNumber}.xml", utf8NoBom);
            fileNumber++;
        }

        await porter.MatchSymbolsAsync(compilation, "TestProject.csproj", isMSBuildProject: false, cts.Token);

        await porter.PortAsync(isMSBuildProject: false, cts.Token);

        foreach ((string resultDocId, IEnumerable<ResolvedLocation> symbolLocations) in porter.GetResults())
        {
            Assert.True(data.TryGetValue(resultDocId, out StringTestData value));
            foreach (ResolvedLocation location in symbolLocations)
            {
                Assert.Equal(value.ExpectedCode, location.NewNode.ToString());
            }
        }
    }
}

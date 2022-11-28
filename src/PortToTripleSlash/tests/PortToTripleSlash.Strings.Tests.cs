// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
    private const string DefaultAssembly = "MyAssembly";

    public PortToTripleSlash_Strings_Tests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public Task Enum_Basic()
    {
        string docId = "T:MyNamespace.MyEnum";

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

        string originalCode = @"namespace MyNamespace;
public enum MyEnum
{
    Value1,
    Value2
}";

        string expectedCode = @"namespace MyNamespace
/// <summary>This is the MyEnum summary.</summary>
/// <remarks>These are the MyEnum remarks.</remarks>
public enum MyEnum
{
    /// <summary>This is the MyEnum.Value1 summary.</summary>
    Value1,
    /// <summary>This is the MyEnum.Value2 summary.</summary>
    Value2
}";


        List<string> docFiles = new() { docFile };
        List<string> originalCodeFiles = new() { originalCode };
        Dictionary<string, string> expectedCodeFiles = new() { { docId, expectedCode } };
        StringTestData stringTestData = new(docFiles, originalCodeFiles, expectedCodeFiles);

        return TestWithStringsAsync(stringTestData);
    }

    [Fact]
    public Task Class_Basic()
    {
        string topLevelTypeDocId = "T:MyNamespace.MyClass";
        string delegateDocId = "T:MyNamespace.MyType.MyDelegate";

        string docFile1 = @"<Type Name=""MyDelegate"" FullName=""MyNamespace.MyType.MyDelegate"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyType.MyDelegate"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <param name=""sender"">This is the MyDelegate sender description.</param>
    <summary>This is the MyDelegate summary.</summary>
    <remarks>These are the MyDelegate remarks.</remarks>
  </Docs>
</Type>
";

        string docFile2 = @"<Type Name=""MyClass"" FullName=""MyNamespace.MyClass"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyClass"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>This is the MyClass summary.</summary>
    <remarks>These are the MyClass remarks.</remarks>
  </Docs>
  <Members>
    <Member MemberName="".ctor"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyClass.#ctor()"" />
      <Docs>
        <summary>This is the MyClass constructor summary.</summary>
        <remarks>These are the MyClass constructor remarks.</remarks>
      </Docs>
    </Member>
    <Member MemberName=""MyVoidMethod"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyClass.MyVoidMethod()"" />
      <Docs>
        <summary>This is the MyVoidMethod summary.</summary>
        <remarks>These are the MyVoidMethod remarks.</remarks>
      </Docs>
    </Member>
    <Member MemberName=""MyIntMethod"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyClass.MyIntMethod(System.Int32)"" />
      <Docs>
        <param name=""withArgument"">This is the MyIntMethod withArgument description.</param>
        <summary>This is the MyIntMethod summary.</summary>
        <returns>This is the MyIntMethod returns description.</returns>
        <remarks>These are the MyIntMethod remarks.</remarks>
      </Docs>
    </Member>
    <Member MemberName=""MyGenericMethod&lt;T&gt;"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyClass.MyGenericMethod`1(`0)"" />
      <Docs>
        <typeparam name=""T"">This is the MyGenericMethod type parameter description.</typeparam>
        <param name=""withGenericArgument"">This is the MyGenericMethod withGenericArgument description.</param>
        <summary>This is the MyGenericMethod summary.</summary>
        <returns>This is the MyGenericMethod returns description.</returns>
        <remarks>These are the MyGenericMethod remarks.</remarks>
      </Docs>
    </Member>
    <Member MemberName=""MyField"">
      <MemberSignature Language=""DocId"" Value=""F:MyNamespace.MyClass.MyField"" />
      <Docs>
        <summary>This is the MyField summary.</summary>
        <remarks>These are the MyField remarks.</remarks>
      </Docs>
    </Member>
    <Member MemberName=""MySetProperty"">
      <MemberSignature Language=""DocId"" Value=""P:MyNamespace.MyClass.MySetProperty"" />
      <Docs>
        <summary>This is the MySetProperty summary.</summary>
        <value>This is the MySetProperty value.</value>
        <remarks>These are the MySetProperty remarks.</remarks>
      </Docs>
    </Member>
    <Member MemberName=""MyGetProperty"">
      <MemberSignature Language=""DocId"" Value=""P:MyNamespace.MyClass.MyGetProperty"" />
      <Docs>
        <summary>This is the MyGetProperty summary.</summary>
        <value>This is the MyGetProperty value.</value>
        <remarks>These are the MyGetProperty remarks.</remarks>
      </Docs>
    </Member>
    <Member MemberName=""MyGetSetProperty"">
      <MemberSignature Language=""DocId"" Value=""P:MyNamespace.MyClass.MyGetSetProperty"" />
      <Docs>
        <summary>This is the MyGetSetProperty summary.</summary>
        <value>This is the MyGetSetProperty value.</value>
        <remarks>These are the MyGetSetProperty remarks.</remarks>
      </Docs>
    </Member>
    <Member MemberName=""MyEvent"">
      <MemberSignature Language=""DocId"" Value=""E:MyNamespace.MyClass.MyEvent"" />
      <Docs>
        <summary>This is the MyEvent summary.</summary>
        <remarks>These are the MyEvent remarks.</remarks>
      </Docs>
    </Member>
    <Member MemberName=""op_Addition"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyClass.op_addition(MyNamespace.MyClass,MyNamespace.MyClass)"" />
      <Docs>
        <param name=""value1"">This is the + operator value1 description.</param>
        <param name=""value2"">This is the + operator value2 description.</param>
        <summary>This is the + operator summary.</summary>
        <returns>This is the + operator returns description.</returns>
        <remarks>These are the + operator remarks.</remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

        string originalCode = @"namespace MyNamespace;
public class MyClass
{
    public MyClass() { }

    public void MyVoidMethod() { }

    public int MyIntMethod(int withArgument) => withArgument;

    public T MyGenericMethod<T>(T withGenericArgument) => withGenericArgument;

    public double MyField;

    public double MySetProperty { set => MyField = value; }

    public double MyGetProperty => MyField;

    public double MyGetSetProperty { get; set; }

    public delegate void MyDelegate(object sender);

    public event MyDelegate MyEvent;

    public static MyClass operator +(MyClass value1, MyClass value2) => value1;
}";

        string expectedCode = @"namespace MyNamespace;
public class MyClass
/// <summary>This is the MyClass summary.</summary>
/// <remarks>These are the MyClass remarks.</remarks>
public class MyClass
{
    /// <summary>This is the MyClass constructor summary.</summary>
    /// <remarks>These are the MyClass constructor remarks.</remarks>
    public MyClass() { }

    /// <summary>This is the MyVoidMethod summary.</summary>
    /// <remarks>These are the MyVoidMethod remarks.</remarks>
    public void MyVoidMethod() { }

    /// <summary>This is the MyIntMethod summary.</summary>
    /// <param name=""withArgument"">This is the MyIntMethod withArgument description.</param>
    /// <returns>This is the MyIntMethod returns description.</returns>
    /// <remarks>These are the MyIntMethod remarks.</remarks>
    public int MyIntMethod(int withArgument) => withArgument;

    /// <summary>This is the MyGenericMethod summary.</summary>
    /// <typeparam name=""T"">This is the MyGenericMethod type parameter description.</typeparam>
    /// <param name=""withGenericArgument"">This is the MyGenericMethod withGenericArgument description.</param>
    /// <returns>This is the MyGenericMethod returns description.</returns>
    /// <remarks>These are the MyGenericMethod remarks.</remarks>
    public T MyGenericMethod<T>(T withGenericArgument) => withGenericArgument;

    /// <summary>This is the MyField summary.</summary>
    /// <remarks>These are the MyField remarks.</remarks>
    public double MyField;

    /// <summary>This is the MySetProperty summary.</summary>
    /// <value>This is the MySetProperty value.</value>
    /// <remarks>These are the MySetProperty remarks.</remarks>
    public double MySetProperty { set => MyField = value; }

    /// <summary>This is the MyGetProperty summary.</summary>
    /// <value>This is the MyGetProperty value.</value>
    /// <remarks>These are the MyGetProperty remarks.</remarks>
    public double MyGetProperty => MyField;

    /// <summary>This is the MyGetSetProperty summary.</summary>
    /// <value>This is the MyGetSetProperty value.</value>
    /// <remarks>These are the MyGetSetProperty remarks.</remarks>
    public double MyGetSetProperty { get; set; }

    /// <summary>This is the MyDelegate summary.</summary>
    /// <param name=""sender"">This is the MyDelegate sender description.</param>
    /// <remarks>These are the MyDelegate remarks.</remarks>
    public delegate void MyDelegate(object sender);

    /// <summary>This is the MyEvent summary.</summary>
    /// <remarks>These are the MyEvent remarks.</remarks>
    public event MyDelegate MyEvent;

    /// <summary>This is the + operator summary.</summary>
    /// <param name=""value1"">This is the + operator value1 description.</param>
    /// <param name=""value2"">This is the + operator value2 description.</param>
    /// <returns>This is the + operator returns description.</returns>
    /// <remarks>These are the + operator remarks.</remarks>
    public static MyClass operator +(MyClass value1, MyClass value2) => value1;
}";

        List<string> docFiles = new() { docFile1, docFile2 };
        List<string> originalCodeFiles = new() { originalCode };
        Dictionary<string, string> expectedCodeFiles = new() { { topLevelTypeDocId, expectedCode }, { delegateDocId, expectedCode } };
        StringTestData stringTestData = new(docFiles, originalCodeFiles, expectedCodeFiles);

        return TestWithStringsAsync(stringTestData);
    }

    private static Task TestWithStringsAsync(StringTestData stringTestData) =>
        TestWithStringsAsync(new Configuration() { SkipInterfaceImplementations = false }, DefaultAssembly, stringTestData);

    private static async Task TestWithStringsAsync(Configuration c, string assembly, StringTestData stringTestData)
    {
        c.IncludedAssemblies.Add(assembly);

        CancellationTokenSource cts = new();

        CSharpParseOptions parseOptions = new CSharpParseOptions().WithKind(SourceCodeKind.Regular);

        CSharpCompilationOptions compileOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            .WithOptimizationLevel(OptimizationLevel.Release).WithAllowUnsafe(enabled: true);

        CSharpCompilation compilation = CSharpCompilation.Create(assembly).WithOptions(compileOptions)
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location)); // reference same mscorlib we're running on

        foreach (string originalCode in stringTestData.OriginalCodeFiles)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(originalCode, parseOptions);
            compilation.AddSyntaxTrees(tree);
        }

        ToTripleSlashPorter porter = new(c);

        UTF8Encoding utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

        int fileNumber = 0;
        foreach (XDocument xDoc in stringTestData.XDocs)
        {
            porter.LoadDocsFile(xDoc, $"File{fileNumber}.xml", utf8NoBom);
            fileNumber++;
        }

        await porter.MatchSymbolsAsync(compilation, $"{assembly}.csproj", isMSBuildProject: false, cts.Token);

        await porter.PortAsync(isMSBuildProject: false, cts.Token);

        IEnumerable<(string, IEnumerable<ResolvedLocation>)> portingResults = porter.GetResults();
        Assert.True(portingResults.Any(), "No items returned in porting results.");
        foreach ((string resultDocId, IEnumerable<ResolvedLocation> symbolLocations) in portingResults)
        {
            Assert.True(stringTestData.ExpectedCodeFiles.TryGetValue(resultDocId, out string expectedCode), $"Could not find docId in dictionary: {resultDocId}");

            foreach (ResolvedLocation location in symbolLocations)
            {
                string newNode = location.NewNode.ToString();
                Assert.Equal(expectedCode, newNode);
            }
        }
    }
}

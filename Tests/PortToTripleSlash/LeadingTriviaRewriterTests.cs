#nullable enable
using Libraries.RoslynTripleSlash;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace Libraries.Tests.PortToTripleSlash
{
    public class LeadingTriviaRewriterTests
    {
        public struct LeadingTriviaTestFile
        {
            public SyntaxNode MyType;
            public SyntaxNode MyEnum;
            public SyntaxNode MyField;
            public SyntaxNode MyProperty;
            public SyntaxNode MyMethod;
            public SyntaxNode MyInterface;
        }

        private static LeadingTriviaTestFile LoadTestFile(string fileName)
        {
            // We rely on the test data files being marked as Content
            // and being copied to the output directory
            string testFolder = "./PortToTripleSlash/TestData/LeadingTrivia";
            string testContent = File.ReadAllText(Path.Combine(testFolder, fileName));

            IEnumerable<SyntaxNode> nodes = SyntaxFactory.ParseSyntaxTree(testContent).GetRoot().DescendantNodes();

            return new LeadingTriviaTestFile
            {
                MyType = nodes.First(n => n.IsKind(SyntaxKind.ClassDeclaration)),
                MyEnum = nodes.First(n => n.IsKind(SyntaxKind.EnumDeclaration)),
                MyField = nodes.First(n => n.IsKind(SyntaxKind.FieldDeclaration)),
                MyProperty = nodes.First(n => n.IsKind(SyntaxKind.PropertyDeclaration)),
                MyMethod = nodes.First(n => n.IsKind(SyntaxKind.MethodDeclaration)),
                MyInterface = nodes.First(n => n.IsKind(SyntaxKind.InterfaceDeclaration))
            };
        }

        private static (LeadingTriviaTestFile Original, LeadingTriviaTestFile Expected) LoadTestFiles(string test)
        {
            LeadingTriviaTestFile original = LoadTestFile($"{test}.Original.cs");
            LeadingTriviaTestFile expected = LoadTestFile($"{test}.Expected.cs");

            return (original, expected);
        }

        public static IEnumerable<object[]> GetLeadingTriviaTests()
        {
            yield return new object[] { "WhitespaceOnly", LoadTestFiles("WhitespaceOnly") };
            yield return new object[] { "Directives", LoadTestFiles("Directives") };
            yield return new object[] { "ExistingXml", LoadTestFiles("ExistingXml") };
            yield return new object[] { "DirectivesExistingXml", LoadTestFiles("DirectivesExistingXml") };
        }

        private static IEnumerable<XmlNodeSyntax> GetTestComments(string testName)
        {
            XmlTextSyntax summaryText = SyntaxFactory.XmlText(testName);
            XmlElementSyntax summaryElement = SyntaxFactory.XmlSummaryElement(summaryText);

            XmlTextSyntax remarksText = SyntaxFactory.XmlText(testName);
            XmlElementSyntax remarksElement = SyntaxFactory.XmlRemarksElement(remarksText);

            return new[] { summaryElement, remarksElement };
        }

        [Fact]
        public void WithoutDocumentationComments_RemovesSingleLineDocumentationComments()
        {
            var trivia = SyntaxFactory.ParseSyntaxTree(@"
                /// <summary>This is the summary</summary>
                /// <remarks>These are the remarks</remarks>
                // This is another comment
                public int field;
                ").GetRoot().GetLeadingTrivia();

            var actual = LeadingTriviaRewriter.WithoutDocumentationComments(trivia).ToFullString();
            var expected = @"
                // This is another comment
                ";

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void WithoutDocumentationComments_RemovesMultiLineDocumentationComments()
        {
            var trivia = SyntaxFactory.ParseSyntaxTree(@"
                /**
                 * <summary>This is the summary</summary>
                 * <remarks>These are the remarks</remarks>
                 * */
                // This is another comment
                public int field;
                ").GetRoot().GetLeadingTrivia();

            var actual = LeadingTriviaRewriter.WithoutDocumentationComments(trivia).ToFullString();
            var expected = @"
                // This is another comment
                ";

            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(GetLeadingTriviaTests))]
        public void AddsXmlToClassDeclaration(string testName, (LeadingTriviaTestFile Original, LeadingTriviaTestFile Expected) test)
        {
            var actual = LeadingTriviaRewriter.ApplyXmlComments(
                test.Original.MyType,
                GetTestComments(testName)
            ).GetLeadingTrivia().ToFullString();

            var expected = test.Expected.MyType.GetLeadingTrivia().ToFullString();

            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(GetLeadingTriviaTests))]
        public void AddsXmlToEnumDeclaration(string testName, (LeadingTriviaTestFile Original, LeadingTriviaTestFile Expected) test)
        {
            var actual = LeadingTriviaRewriter.ApplyXmlComments(
                test.Original.MyEnum,
                GetTestComments(testName)
            ).GetLeadingTrivia().ToFullString();

            var expected = test.Expected.MyEnum.GetLeadingTrivia().ToFullString();

            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(GetLeadingTriviaTests))]
        public void AddsXmlToFieldDeclaration(string testName, (LeadingTriviaTestFile Original, LeadingTriviaTestFile Expected) test)
        {
            var actual = LeadingTriviaRewriter.ApplyXmlComments(
                test.Original.MyField,
                GetTestComments(testName)
            ).GetLeadingTrivia().ToFullString();

            var expected = test.Expected.MyField.GetLeadingTrivia().ToFullString();

            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(GetLeadingTriviaTests))]
        public void AddsXmlToPropertyDeclaration(string testName, (LeadingTriviaTestFile Original, LeadingTriviaTestFile Expected) test)
        {
            var actual = LeadingTriviaRewriter.ApplyXmlComments(
                test.Original.MyProperty,
                GetTestComments(testName)
            ).GetLeadingTrivia().ToFullString();

            var expected = test.Expected.MyProperty.GetLeadingTrivia().ToFullString();

            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(GetLeadingTriviaTests))]
        public void AddsXmlToMethodDeclaration(string testName, (LeadingTriviaTestFile Original, LeadingTriviaTestFile Expected) test)
        {
            var actual = LeadingTriviaRewriter.ApplyXmlComments(
                test.Original.MyMethod,
                GetTestComments(testName)
            ).GetLeadingTrivia().ToFullString();

            var expected = test.Expected.MyMethod.GetLeadingTrivia().ToFullString();

            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(GetLeadingTriviaTests))]
        public void AddsXmlToInterfaceDeclaration(string testName, (LeadingTriviaTestFile Original, LeadingTriviaTestFile Expected) test)
        {
            var actual = LeadingTriviaRewriter.ApplyXmlComments(
                test.Original.MyInterface,
                GetTestComments(testName)
            ).GetLeadingTrivia().ToFullString();

            var expected = test.Expected.MyInterface.GetLeadingTrivia().ToFullString();

            Assert.Equal(expected, actual);
        }
    }
}
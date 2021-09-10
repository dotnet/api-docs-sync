using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Libraries.RoslynTripleSlash
{
    public static class LeadingTriviaRewriter
    {
        private static int[] TriviaAboveDocComments = new[]
        {
            (int)SyntaxKind.RegionDirectiveTrivia,
            (int)SyntaxKind.PragmaWarningDirectiveTrivia,
            (int)SyntaxKind.IfDirectiveTrivia,
            (int)SyntaxKind.EndIfDirectiveTrivia,
        };

        public static int[] TriviaBelowDocComments = new[]
        {
            (int)SyntaxKind.SingleLineCommentTrivia,
            (int)SyntaxKind.MultiLineCommentTrivia
        };

        private static bool IsDocumentationCommentTrivia(this SyntaxTrivia trivia) =>
            trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
            trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia);

        private static bool IsDocumentationCommentTriviaContinuation(this SyntaxTrivia trivia) =>
            trivia.IsDocumentationCommentTrivia() ||
            trivia.IsKind(SyntaxKind.EndOfLineTrivia) ||
            trivia.IsKind(SyntaxKind.WhitespaceTrivia);

        public static SyntaxTriviaList WithoutDocumentationComments(this SyntaxTriviaList trivia)
        {
            return trivia.WithoutDocumentationComments(out int? _);
        }

        public static SyntaxTriviaList GetFinalWhitespace(this SyntaxTriviaList trivia)
        {
            SyntaxTriviaList indentation = new();
            int index = trivia.Count;
            
            while (index > 0 && trivia[index - 1].IsKind(SyntaxKind.WhitespaceTrivia))
            {
                index--;
                indentation = indentation.Insert(0, trivia[index]);
            }

            return indentation;
        }

        public static SyntaxTriviaList WithoutDocumentationComments(this SyntaxTriviaList trivia, out int? existingDocsPosition)
        {
            int i = 0;
            existingDocsPosition = null;

            // Before we start removing the doc comments, we need to capture any whitespace at
            // the very end of the trivia, because it could represent indentation of the API.
            SyntaxTriviaList indentation = trivia.GetFinalWhitespace();

            while (i < trivia.Count)
            {
                if (trivia[i].IsDocumentationCommentTrivia())
                {
                    var commentStart = i;
                    var commentEnd = i;

                    // Walk backward through whitespace to find the beginning of the doc comment
                    // Now walk the doc comment position backward through any of its indentation trivia
                    while (commentStart > 0 && trivia[commentStart - 1].IsKind(SyntaxKind.WhitespaceTrivia))
                    {
                        commentStart--;
                    }

                    // Walk forward to find the end of the doc comment, but do not go past the
                    // beginning of the API documentation.
                    while (commentEnd < trivia.Count - indentation.Count && trivia[commentEnd + 1].IsDocumentationCommentTriviaContinuation())
                    {
                        commentEnd++;
                    }

                    // Finally, walk the end position backthrough any indentation (for other
                    // lines before the API itself).
                    while (commentEnd >= commentStart && trivia[commentEnd].IsKind(SyntaxKind.WhitespaceTrivia))
                    {
                        commentEnd--;
                    }

                    // Remove the trivia from beginning to end of this doc comment
                    while (commentEnd >= commentStart)
                    {
                        trivia = trivia.RemoveAt(commentStart);
                        commentEnd--;
                    }

                    // Capture the first documentation comment position
                    // If there were disjoint doc comments, we will
                    // anchor on the first occurrence
                    existingDocsPosition ??= commentStart;
                }

                i++;
            }

            return trivia;
        }

        public static SyntaxNode ApplyXmlComments(SyntaxNode node, IEnumerable<XmlNodeSyntax> xmlComments)
        {
            if (!node.HasLeadingTrivia)
            {
                return node.WithLeadingTrivia(GetXmlCommentLines(xmlComments));
            }

            SyntaxTriviaList leading = node.GetLeadingTrivia().WithoutDocumentationComments(out int? docsPosition);

            if (docsPosition is null)
            {
                // We will determine the position at which to insert the XML
                // comments. We want to find the spot closest to the declaration
                // that makes sense, so we walk upward through the leading trivia
                // until we find nodes we need to stay beneath. Then, we walk back
                // downward until we find the first node to stay above.
                docsPosition = leading.Count;

                while (docsPosition > 0 && !TriviaAboveDocComments.Contains(leading[docsPosition.Value - 1].RawKind))
                {
                    docsPosition--;
                }

                while (docsPosition < leading.Count && !TriviaBelowDocComments.Contains(leading[docsPosition.Value].RawKind))
                {
                    docsPosition++;
                }

                // The last step is to walk backwards again through any whitespace
                // to get back to the beginning of the line.
                while (docsPosition > 0 && leading[docsPosition.Value - 1].IsKind(SyntaxKind.WhitespaceTrivia))
                {
                    docsPosition--;
                }
            }

            // We know where the doc comments will be inserted, but they could go in adjacent to
            // pragmas or other trivia where the indentation might not match the API being
            // documented. Look at the end of the trivia (just before the API), and clone the
            // indentation for use in front of each line of documentation comments.
            SyntaxTriviaList indentation = leading.GetFinalWhitespace();

            // Insert the XML comment lines with the collected indentation
            return node.WithLeadingTrivia(
                leading.InsertRange(docsPosition.Value, GetXmlCommentLines(xmlComments, indentation))
            );
        }

        public static SyntaxTriviaList GetXmlCommentLines(IEnumerable<XmlNodeSyntax> xmlComments, SyntaxTriviaList indentation = new())
        {
            SyntaxTriviaList xmlTrivia = new();

            foreach (var xmlComment in xmlComments)
            {
                var lines = xmlComment.ToString().Split(Environment.NewLine);

                var commentLines = lines.Select((l, i) => {
                    var text = XmlText(XmlTextLiteral(l, l));
                    var comment = DocumentationComment(text);
                    var leadingTrivia = comment.GetLeadingTrivia().InsertRange(0, indentation);

                    return Trivia(comment.WithLeadingTrivia(leadingTrivia).WithTrailingTrivia(CarriageReturnLineFeed));
                });

                xmlTrivia = xmlTrivia.AddRange(commentLines);
            }

            return xmlTrivia;
        }
    }
}

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using static System.Net.WebUtility;

namespace Libraries.Docs
{
    public class DocsApiReference
    {
        public bool IsOverload { get; private init; }

        public char? Prefix { get; private init; }

        public string Api { get; private init; }

        // Generic parameters need to support both single and double backtick conventions
        private const string GenericParameterPattern = @"`{1,2}(?<arity>\d+)";
        private const string ApiChars = @"[A-Za-z0-9\-\._~:\/#\[\]\{\}@!\$&'\(\)\*\+,;`]";
        private const string ApiReferencePattern = @"((?<prefix>[A-Za-z]):)?(?<api>(" + ApiChars + @")+)?(?<extraVars>\?(" + ApiChars + @")+=(" + ApiChars + @")+)?";
        private static readonly Regex XrefPattern = new("<xref:(?<api>" + ApiReferencePattern + ")\\s*>", RegexOptions.Compiled);

        public DocsApiReference(string apiReference)
        {
            Api = UrlDecode(apiReference);
            var match = Regex.Match(Api, ApiReferencePattern);

            if (match.Success)
            {
                Api = match.Groups["api"].Value;

                if (match.Groups["prefix"].Success)
                {
                    Prefix = match.Groups["prefix"].Value[0];
                    IsOverload = Prefix == 'O';
                }
            }

            if (Api.EndsWith('*'))
            {
                IsOverload = true;
                Api = Api[..^1];
            }

            Api = ReplacePrimitivesWithShorthands(Api);
            Api = ParseGenericTypes(Api);
        }

        public override string ToString()
        {
            if (Prefix is not null)
            {
                return $"{Prefix}:{Api}";
            }

            return Api;
        }

        private static readonly Dictionary<string, string> PrimitiveTypes = new()
        {
            { "System.Boolean", "bool" },
            { "System.Byte", "byte" },
            { "System.Char", "char" },
            { "System.Decimal", "decimal" },
            { "System.Double", "double" },
            { "System.Int16", "short" },
            { "System.Int32", "int" },
            { "System.Int64", "long" },
            { "System.Object", "object" }, // Ambiguous: could be 'object' or 'dynamic' https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/built-in-types
            { "System.SByte", "sbyte" },
            { "System.Single", "float" },
            { "System.String", "string" },
            { "System.UInt16", "ushort" },
            { "System.UInt32", "uint" },
            { "System.UInt64", "ulong" },
            { "System.Void", "void" }
        };

        public static string ReplacePrimitivesWithShorthands(string apiReference)
        {
            foreach ((string key, string value) in PrimitiveTypes)
            {
                apiReference = Regex.Replace(apiReference, key, value);
            }

            return apiReference;
        }

        public static string ParseGenericTypes(string apiReference)
        {
            int genericParameterArity = 0;
            return Regex.Replace(apiReference, GenericParameterPattern, MapGenericParameter);

            string MapGenericParameter(Match match)
            {
                int arity = int.Parse(match.Groups["arity"].Value);

                if (genericParameterArity == 0)
                {
                    // This is the first match that declares the generic parameter arity of the method
                    // e.g. GenericMethod``3 ---> GenericMethod{T1,T2,T3}(...);
                    Debug.Assert(arity > 0);
                    genericParameterArity = arity;
                    return WrapInCurlyBrackets(string.Join(",", Enumerable.Range(0, arity).Select(CreateGenericParameterName)));
                }

                // Subsequent matches are references to generic parameters in the method signature,
                // e.g. GenericMethod{T1,T2,T3}(..., List{``1} parameter, ...); ---> List{T2} parameter
                return CreateGenericParameterName(arity);

                // This naming scheme does not map to the exact generic parameter names;
                // however this is still accepted by intellisense and backporters can rename
                // manually with the help of tooling.
                string CreateGenericParameterName(int index) => genericParameterArity == 1 ? "T" : $"T{index + 1}";

                static string WrapInCurlyBrackets(string input) => $"{{{input}}}";
            }
        }

        public static string ReplaceMarkdownXrefWithSeeCref(string markdown)
        {
            return XrefPattern.Replace(markdown, match =>
            {
                var api = new DocsApiReference(match.Groups["api"].Value);
                return @$"<see cref=""{api}"" />";
            });
        }
    }
}

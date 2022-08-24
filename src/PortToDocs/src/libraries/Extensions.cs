// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace ApiDocsSync.Libraries
{
    // Provides generic extension methods.
    internal static class Extensions
    {
        // Removes the specified substrings from another string
        public static string RemoveSubstrings(this string oldString, params string[] stringsToRemove)
        {
            string newString = oldString;
            foreach (string toRemove in stringsToRemove)
            {
                if (newString.Contains(toRemove))
                {
                    newString = newString.Replace(toRemove, string.Empty);
                }
            }
            return newString;
        }

        public static bool ContainsStrings(this string text, string[] strings)
        {
            foreach (string str in strings)
            {
                if (text.Contains(str))
                {
                    return true;
                }
            }

            return false;
        }

        // Some API DocIDs with types contain "{" and "}" to enclose the typeparam, which causes
        // an exception to be thrown when trying to embed the string in a formatted string.
        public static string AsEscapedDocId(this string docId) =>
            docId
            .Replace("{", "{{")
            .Replace("}", "}}")
            .Replace("<", "{{")
            .Replace(">", "}}")
            .Replace("&lt;", "{{")
            .Replace("&gt;", "}}");

        // Checks if the passed string is considered "empty" according to the Docs repo rules.
        public static bool IsDocsEmpty(this string? s) =>
            string.IsNullOrWhiteSpace(s) || s == Configuration.ToBeAdded;
    }

}

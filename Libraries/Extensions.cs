#nullable enable
using System.Collections.Generic;

namespace Libraries
{
    // Provides generic extension methods.
    internal static class Extensions
    {
        // Adds a string to a list of strings if the element is not there yet. The method makes sure to escape unexpected curly brackets to prevent formatting exceptions.
        public static void AddIfNotExists(this List<string> list, string element)
        {
            string cleanedElement = element.Escaped();
            if (!list.Contains(cleanedElement))
            {
                list.Add(cleanedElement);
            }
        }

        // Removes the specified subtrings from another string
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

        // Some API DocIDs with types contain "{" and "}" to enclose the typeparam, which causes
        // an exception to be thrown when trying to embed the string in a formatted string.
        public static string Escaped(this string str) => str.Replace("{", "{{").Replace("}", "}}");

        // Checks if the passed string is considered "empty" according to the Docs repo rules.
        public static bool IsDocsEmpty(this string? s) =>
            string.IsNullOrWhiteSpace(s) || s == Configuration.ToBeAdded;
    }

}

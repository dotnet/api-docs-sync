using System.Collections.Generic;

namespace DocsPortingTool
{
    // Provides generic extension methods.
    public static class Extensions
    {
        // Adds a string to a list of strings if the element is not there yet. The method makes sure to escape unexpected curly brackets to prevent formatting exceptions.
        public static void AddIfNotExists(this List<string> list, string element)
        {
            string cleanedElement = element.Replace("{", "{{").Replace("}", "}}");
            if (!list.Contains(cleanedElement))
            {
                list.Add(cleanedElement);
            }
        }

        // Removes the specified string from a remarks string
        public static string CleanRemarksText(this string oldRemarks, string toRemove)
        {
            if (oldRemarks.Contains(toRemove))
            {
                return oldRemarks.Replace(toRemove, string.Empty);
            }
            return oldRemarks;
        }

        // Some API DocIDs with types contain "{" and "}" to enclose the typeparam, which causes
        // an exception to be thrown when trying to embed the string in a formatted string.
        public static string Escaped(this string str) => str.Replace("{", "{{").Replace("}", "}}");
    }

}

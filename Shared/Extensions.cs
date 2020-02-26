using System.Collections.Generic;

namespace DocsPortingTool
{
    /// <summary>
    /// Provides generic extension methods.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Adds a string to a list of strings if the element is not there yet. The method makes sure to escape unexpected curly brackets to prevent formatting exceptions.
        /// </summary>
        /// <param name="list">A string list.</param>
        /// <param name="element">A string.</param>
        public static void AddIfNotExists(this List<string> list, string element)
        {
            string cleanedElement = element.Replace("{", "{{").Replace("}", "}}");
            if (!list.Contains(cleanedElement))
            {
                list.Add(cleanedElement);
            }
        }
    }

}

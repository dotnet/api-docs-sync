// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace ApiDocsSync.PortToTripleSlash
{
    internal class XmlHelper
    {
        private static readonly (string, string)[] ReplaceableMarkdownPatterns = new[]
        {
            (@"\s*<!\[CDATA\[\s*", ""),
            (@"\s*\]\]>\s*", ""),
            (@"\s*##\s*Remarks\s*", ""),
            (@"`(?'keyword'null|false|true)`", "<see langword=\"${keyword}\" />"),
            (@"<c>(?'keyword'null|false|true)</c>", "<see langword=\"${keyword}\" />"),
            (@"<xref:(?'docId'[a-zA-Z0-9\._\@\#\$\%\(\)\[\]<>\?\,]+)>", "<see cref=\"${docId}\" />"),
            (@"%601", "{T}")
        };

        public static string GetAttributeValue(XElement parent, string name)
        {
            if (parent == null)
            {
                throw new Exception($"A null parent was passed when attempting to get attribute '{name}'");
            }
            else
            {
                XAttribute? attr = parent.Attribute(name);
                if (attr != null)
                {
                    return attr.Value.Trim();
                }
            }
            return string.Empty;
        }

        public static bool TryGetChildElement(XElement parent, string name, out XElement? child)
        {
            child = null;

            if (parent == null || string.IsNullOrWhiteSpace(name))
                return false;

            child = parent.Element(name);

            return child != null;
        }

        public static string GetChildElementValue(XElement parent, string childName)
        {
            XElement? child = parent.Element(childName);

            if (child != null)
            {
                return GetNodesInPlainText(childName, child);
            }

            return string.Empty;
        }

        public static string GetNodesInPlainText(string name, XElement element)
        {
            if (element == null)
            {
                throw new Exception("A null element was passed when attempting to retrieve the nodes in plain text.");
            }

            if (name == "remarks")
            {
                XElement? formatElement = element.Element("format");
                if (formatElement != null)
                {
                    element = formatElement;
                }
            }
            // string.Join("", element.Nodes()) is very slow.
            //
            // The following is twice as fast (although still slow)
            // but does not produce the same spacing. That may be OK.
            //
            using XmlReader reader = element.CreateReader();
            reader.MoveToContent();
            string actualValue = reader.ReadInnerXml().Trim();

            if (name == "remarks")
            {
                actualValue = ReplaceMarkdown(actualValue);
            }

            return actualValue.IsDocsEmpty() ? string.Empty : actualValue;
        }

        private static string ReplaceMarkdown(string value)
        {
            foreach ((string bad, string good) in ReplaceableMarkdownPatterns)
            {
                value = Regex.Replace(value, bad, good);
            }

            return string.Join(Environment.NewLine, value.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }
    }
}

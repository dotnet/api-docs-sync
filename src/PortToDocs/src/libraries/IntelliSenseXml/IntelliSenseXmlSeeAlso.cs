// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;

namespace ApiDocsSync.PortToDocs.IntelliSenseXml;

internal class IntelliSenseXmlSeeAlso(XElement xeSeeAlso)
{
    public XElement XESeeAlso
    {
        get;
        private set;
    } = xeSeeAlso;

    private string _cref = string.Empty;
    public string Cref
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_cref))
            {
                _cref = XmlHelper.GetAttributeValue(XESeeAlso, "cref");
            }
            return _cref;
        }
    }

    public override string ToString() => $"SeeAlso cref={Cref}";
}

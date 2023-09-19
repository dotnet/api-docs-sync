// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;

namespace ApiDocsSync.PortToDocs.Docs;

internal class DocsSeeAlso
{
    private readonly XElement XESeeAlso;

    public IDocsAPI ParentAPI
    {
        get; private set;
    }

    public string Cref => XmlHelper.GetAttributeValue(XESeeAlso, "cref");

    public DocsSeeAlso(IDocsAPI parentAPI, XElement xSeeAlso)
    {
        ParentAPI = parentAPI;
        XESeeAlso = xSeeAlso;
    }

    public override string ToString() => $"seealso cref={Cref}";
}

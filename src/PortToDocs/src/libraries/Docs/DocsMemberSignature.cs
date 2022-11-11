// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;

namespace ApiDocsSync.PortToDocs.Docs
{
    internal class DocsMemberSignature
    {
        private readonly XElement XEMemberSignature;

        public string Language
        {
            get
            {
                return XmlHelper.GetAttributeValue(XEMemberSignature, "Language");
            }
        }

        public string Value
        {
            get
            {
                return XmlHelper.GetAttributeValue(XEMemberSignature, "Value");
            }
        }

        public DocsMemberSignature(XElement xeMemberSignature)
        {
            XEMemberSignature = xeMemberSignature;
        }
    }
}

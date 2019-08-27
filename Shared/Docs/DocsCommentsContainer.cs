using Shared;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

/*
root: Type 1:1
    TypeSignature 1:M
    AssemblyInfo 1:M
        AssemblyName 1:1
        AssemblyVersion 1:M
    Base 1:1
        BaseTypeName 1:1
    Interfaces 0:1
        Interface 0:1
            InterfaceName 1:1
    Docs 1:1
        summary 1:1
        remarks 1:1
    Members 1:1
        Member 1:1
            MemberSignature 1:M
            MemberType 1:1
            AssemblyInfo 1:M
                AssemblyName 1:1
                AssemblyVersion 1:M
            ReturnValue 1:1
                ReturnType 1:1
            Parameters 0:1
                Parameter 1:M
            TypeParameters 0:1
                TypeParameter 1:M
            Docs 1:1
                param 0:M // One for each Parameter above
                summary 1:1
                returns 0:1
                remarks 0:1
                typeparam 0:M // One for each TypeParameter above
*/
namespace DocsPortingTool.Docs
{
    public class DocsCommentsContainer
    {
        private XDocument xDoc = null;

        public readonly List<DocsType> Containers = new List<DocsType>();
        public readonly List<DocsMember> Members = new List<DocsMember>();

        public DocsCommentsContainer()
        {
        }

        

        public void LoadFile(FileInfo fileInfo, Configuration config)
        {
            if (!fileInfo.Exists)
            {
                Log.Error(string.Format("Docs xml file does not exist: {0}", fileInfo.FullName));
                return;
            }

            xDoc = XDocument.Load(fileInfo.FullName);

            if (xDoc.Root == null)
            {
                Log.Error("Docs xml file does not have a root element: {0}", fileInfo.FullName);
                return;
            }

            if (xDoc.Root.Name != "Type")
            {
                Log.Error("Docs xml file does not have a 'Type' root element: {0}", fileInfo.FullName);
                return;
            }

            if (!xDoc.Root.HasElements)
            {
                Log.Error("Docs xml file Type element does not have any children: {0}", fileInfo.FullName);
                return;
            }

            if (xDoc.Root.Elements("Docs").Count() != 1)
            {
                Log.Error("Docs xml file Type element does not have a Docs child: {0}", fileInfo.FullName);
                return;
            }

            DocsType docsType = new DocsType(fileInfo.FullName, xDoc, xDoc.Root);

            bool add = false;
            foreach (string included in config.IncludedAssemblies)
            {
                if (docsType.AssemblyInfos.Count(x => x.AssemblyName.StartsWith(included)) > 0)
                {
                    add = true;
                    break;
                }
            }

            foreach (string excluded in config.ExcludedAssemblies)
            {
                if (docsType.AssemblyInfos.Count(x => x.AssemblyName.StartsWith(excluded)) > 0)
                {
                    add = false;
                    Log.Warning("Docs xml file excluded: {0}", fileInfo.FullName);
                    break;
                }
            }

            if (add)
            {
                Containers.Add(docsType);

                XElement xeMembers = XmlHelper.GetChildElement(xDoc.Root, "Members");

                if (xeMembers != null)
                {
                    foreach (XElement xeMember in xeMembers.Elements("Member"))
                    {
                        DocsMember member = new DocsMember(fileInfo.FullName, xDoc, xeMember);
                        Members.Add(member);
                    }
                }

                Log.Success("Docs xml file included: {0}", fileInfo.FullName);
            }
        }
    }
}

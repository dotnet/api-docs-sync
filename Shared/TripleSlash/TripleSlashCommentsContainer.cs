using Shared;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

/*
The triple slash comments xml files for...
A) corefx are saved in:
    corefx/artifacts/bin/<namespace>
B) coreclr are saved in:
    coreclr\packages\microsoft.netcore.app\<version>\ref\netcoreapp<version>\
    or in:
        corefx/artifacts/bin/docs
        but in this case, only namespaces found in coreclr/src/System.Private.CoreLib/shared need to be searched here.

Each xml file represents a namespace.
The files are structured like this:

root
    assembly (1)
        name (1)
    members (many)
        member(0:M)
            summary (0:1)
            param (0:M)
            returns (0:1)
            exception (0:M)
                Note: The exception value may contain xml nodes.
*/
namespace DocsPortingTool.TripleSlash
{
    public class TripleSlashCommentsContainer
    {
        private XDocument xDoc = null;

        public List<TripleSlashMember> Members = new List<TripleSlashMember>();

        public TripleSlashCommentsContainer()
        {
        }

        public void LoadFile(FileInfo fileInfo, List<string> includedAssemblies, List<string> excludedAssemblies, bool printSuccess)
        {
            if (!fileInfo.Exists)
            {
                Log.Error(string.Format("Triple slash xml file does not exist: {0}", fileInfo.FullName));
                return;
            }

            xDoc = XDocument.Load(fileInfo.FullName);

            if (xDoc.Root == null)
            {
                Log.Error("Triple slash xml file does not contain a root element: {0}", fileInfo.FullName);
                return;
            }

            if (xDoc.Root.Name != "doc")
            {
                Log.Error("Triple slash xml file does not contain a doc element: {0}", fileInfo.FullName);
                return;
            }

            if (!xDoc.Root.HasElements)
            {
                Log.Error("Triple slash xml file doc element not have any children: {0}", fileInfo.FullName);
                return;
            }

            if (xDoc.Root.Elements("assembly").Count() != 1)
            {
                Log.Error("Tripls slash xml file does not contain exactly 1 'assembly' element: {0}", fileInfo.FullName);
                return;
            }

            if (xDoc.Root.Elements("members").Count() != 1)
            {
                Log.Error("Triple slash xml file does not contain exactly 1 'members' element: {0}", fileInfo.FullName);
                return;
            }

            XElement xeMembers = XmlHelper.GetChildElement(xDoc.Root, "members");

            foreach (XElement xeMember in xeMembers.Elements("member"))
            {
                TripleSlashMember member = new TripleSlashMember(xeMember);

                bool add = false;
                foreach (string included in includedAssemblies)
                {
                    if (member.Assembly.StartsWith(included))
                    {
                        add = true;
                        break;
                    }
                }

                foreach (string excluded in excludedAssemblies)
                {
                    if (member.Assembly.StartsWith(excluded))
                    {
                        add = false;
                        break;
                    }
                }

                if (add)
                {
                    Members.Add(member);
                }
            }

            if (printSuccess)
            {
                Log.Success("Triple slash xml file included: {0}", fileInfo.FullName);
            }
        }
    }
}

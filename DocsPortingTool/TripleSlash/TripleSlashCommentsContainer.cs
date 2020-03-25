#nullable enable
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
        private Configuration Config { get; set; }

        private XDocument? xDoc = null;

        public List<TripleSlashMember> Members = new List<TripleSlashMember>();

        public int TotalFiles
        {
            get
            {
                return Members.Count;
            }
        }

        public TripleSlashCommentsContainer(Configuration config)
        {
            Config = config;
        }

        public void CollectFiles()
        {
            foreach (FileInfo fileInfo in EnumerateFiles())
            {
                LoadFile(fileInfo, printSuccess: true);
            }
            Log.Line();
        }

        private List<FileInfo> EnumerateFiles()
        {
            Log.Info("Looking for triple slash xml files...");

            List<FileInfo> fileInfos = new List<FileInfo>();

            foreach (DirectoryInfo dirInfo in Config.DirsTripleSlashXmls)
            {
                // 1) Find all the xml files inside all the subdirectories inside the triple slash xml directory
                foreach (DirectoryInfo subDir in dirInfo.EnumerateDirectories("*", SearchOption.TopDirectoryOnly))
                {
                    if (!Configuration.ForbiddenDirectories.Contains(subDir.Name) && !subDir.Name.EndsWith(".Tests"))
                    {
                        foreach (FileInfo fileInfo in subDir.EnumerateFiles("*.xml", SearchOption.AllDirectories))
                        {
                            fileInfos.Add(fileInfo);
                        }
                    }
                }

                // 2) Find all the xml files in the top directory
                foreach (FileInfo fileInfo in dirInfo.EnumerateFiles("*.xml", SearchOption.TopDirectoryOnly))
                {
                    fileInfos.Add(fileInfo);
                }
            }

            Log.Success("Finished looking for triple slash xml files.");

            return fileInfos;
        }

        private void LoadFile(FileInfo fileInfo, bool printSuccess)
        {
            if (!fileInfo.Exists)
            {
                Log.Error($"Triple slash xml file does not exist: {fileInfo.FullName}");
                return;
            }

            xDoc = XDocument.Load(fileInfo.FullName);

            if (xDoc.Root == null)
            {
                Log.Error($"Triple slash xml file does not contain a root element: {fileInfo.FullName}");
                return;
            }

            if (xDoc.Root.Name != "doc")
            {
                Log.Error($"Triple slash xml file does not contain a doc element: {fileInfo.FullName}");
                return;
            }

            if (!xDoc.Root.HasElements)
            {
                Log.Error($"Triple slash xml file doc element not have any children: {fileInfo.FullName}");
                return;
            }

            if (xDoc.Root.Elements("assembly").Count() != 1)
            {
                Log.Error($"Triple slash xml file does not contain exactly 1 'assembly' element: {fileInfo.FullName}");
                return;
            }

            XElement xAssembly = xDoc.Root.Element("assembly");
            if (xAssembly.Elements("name").Count() != 1)
            {
                Log.Error($"Triple slash xml file assembly element does not contain exactly 1 'name' element: {fileInfo.FullName}");
                return;
            }

            string assembly = xAssembly.Element("name").Value;
            if (string.IsNullOrEmpty(assembly))
            {
                Log.Error($"Triple slash xml file assembly string is null or empty: {fileInfo.FullName}");
            }

            if (xDoc.Root.Elements("members").Count() != 1)
            {
                Log.Error($"Triple slash xml file does not contain exactly 1 'members' element: {fileInfo.FullName}");
                return;
            }

            int totalAdded = 0;
            if (XmlHelper.TryGetChildElement(xDoc.Root, "members", out XElement xeMembers))
            {
                foreach (XElement xeMember in xeMembers.Elements("member"))
                {
                    TripleSlashMember member = new TripleSlashMember(xeMember, assembly);

                    bool add = false;
                    foreach (string included in Config.IncludedAssemblies)
                    {
                        if (member.Assembly.StartsWith(included) ||
                            member.Name.Substring(2).StartsWith(included) ||
                            member.Namespace.StartsWith(included) ||
                            Configuration.ReplaceNamespace(member.Assembly).StartsWith(included))
                        {
                            add = true;
                            break;
                        }
                    }

                    foreach (string excluded in Config.ExcludedAssemblies)
                    {
                        if (member.Assembly.StartsWith(excluded) || member.Name.Substring(2).StartsWith(excluded))
                        {
                            add = false;
                            break;
                        }
                    }

                    if (add)
                    {
                        totalAdded++;
                        Members.Add(member);
                    }
                }
            }

            if (printSuccess && totalAdded > 0)
            {
                Log.Success($"{totalAdded} triple slash member(s) added from xml file '{fileInfo.FullName}'");
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
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

        public readonly List<DocsType> Types = new List<DocsType>();
        public readonly List<DocsMember> Members = new List<DocsMember>();

        public DocsCommentsContainer()
        {
        }

        public void CollectFiles()
        {

            foreach (FileInfo fileInfo in EnumerateFiles())
            {
                LoadFile(fileInfo);
            }
        }

        public void Save()
        {
            if (Configuration.Save)
            {
                List<string> savedFiles = new List<string>();
                foreach (var type in Types.Where(x => x.Changed))
                {
                    Log.Warning(false, $"Saving changes for {type.FilePath}:");

                    try
                    {
                        // These settings prevent the addition of the <xml> element on the first line and will preserve indentation+endlines
                        XmlWriterSettings xws = new XmlWriterSettings { OmitXmlDeclaration = true, Indent = true, Encoding = Encoding.GetEncoding("ISO-8859-1") };
                        using (XmlWriter xw = XmlWriter.Create(type.FilePath, xws))
                        {
                            type.XDoc.Save(xw);
                        }

                        // Workaround to delete the annoying endline added by XmlWriter.Save
                        string fileData = File.ReadAllText(type.FilePath);
                        if (!fileData.EndsWith(Environment.NewLine))
                        {
                            File.WriteAllText(type.FilePath, fileData + Environment.NewLine);
                        }

                        Log.Success(" [Saved]");
                    }
                    catch (Exception e)
                    {
                        Log.Error(e.Message);
                        Log.Line();
                        Log.Error(e.StackTrace);
                        if (e.InnerException != null)
                        {
                            Log.Line();
                            Log.Error(e.InnerException.Message);
                            Log.Line();
                            Log.Error(e.InnerException.StackTrace);
                        }
                        System.Threading.Thread.Sleep(1000);
                    }

                    Log.Line();
                }
            }
        }

        private bool HasAllowedName(FileInfo fileInfo)
        {
            return !fileInfo.Name.StartsWith("ns-") && fileInfo.Name != "index.xml";
        }

        private List<FileInfo> EnumerateFiles()
        {
            Log.Info("Looking for Docs xml files...");

            List<FileInfo> fileInfos = new List<FileInfo>();

            foreach (DirectoryInfo subDir in Configuration.DirDocsXml.EnumerateDirectories("*", SearchOption.TopDirectoryOnly))
            {
                if (!Configuration.ForbiddenDirectories.Contains(subDir.Name) && !subDir.Name.EndsWith(".Tests"))
                {
                    foreach (FileInfo fileInfo in subDir.EnumerateFiles("*.xml", SearchOption.AllDirectories))
                    {
                        if (Configuration.HasAllowedAssemblyPrefix(subDir.Name) && HasAllowedName(fileInfo))
                        {
                            fileInfos.Add(fileInfo);
                        }
                    }
                }
            }

            // Make sure to include the files in the base directory too
            foreach (FileInfo fileInfo in Configuration.DirDocsXml.EnumerateFiles("*.xml", SearchOption.TopDirectoryOnly))
            {
                if (HasAllowedName(fileInfo))
                {
                    fileInfos.Add(fileInfo);
                }
            }

            Log.Success("Finished looking for Docs xml files");

            return fileInfos;
        }

        private void LoadFile(FileInfo fileInfo)
        {
            if (!fileInfo.Exists)
            {
                Log.Error($"Docs xml file does not exist: {fileInfo.FullName}");
                return;
            }

            if (fileInfo.Name.StartsWith("ns-"))
            {
                Log.Warning($"Skipping namespace file: {fileInfo.FullName}");
                return;
            }

            xDoc = XDocument.Load(fileInfo.FullName);

            if (xDoc.Root == null)
            {
                Log.Error($"Docs xml file does not have a root element: {fileInfo.FullName}");
                return;
            }

            if (xDoc.Root.Name == "Namespace")
            {
                Log.Error($"Skipping namespace file (should have been filtered already): {fileInfo.FullName}");
                return;
            }

            if (xDoc.Root.Name != "Type")
            {
                Log.Error($"Docs xml file does not have a 'Type' root element: {fileInfo.FullName}");
                return;
            }

            if (!xDoc.Root.HasElements)
            {
                Log.Error($"Docs xml file Type element does not have any children: {fileInfo.FullName}");
                return;
            }

            if (xDoc.Root.Elements("Docs").Count() != 1)
            {
                Log.Error($"Docs xml file Type element does not have a Docs child: {fileInfo.FullName}");
                return;
            }

            DocsType docsType = new DocsType(fileInfo.FullName, xDoc, xDoc.Root);

            bool add = false;
            // Add all interfaces in case EIIs are found
            if (docsType.Name.StartsWith('I'))
            {
                Log.Success($" - Docs interface included: {docsType.Name}");
                add = true;
            }
            else
            {
                foreach (string included in Configuration.IncludedAssemblies)
                {
                    if (docsType.AssemblyInfos.Count(x => x.AssemblyName.StartsWith(included)) > 0 || docsType.FullName.StartsWith(included))
                    {
                        add = true;

                        if (Configuration.IncludedTypes.Count() > 0)
                        {
                            if (!Configuration.IncludedTypes.Contains(docsType.Name))
                            {
                                add = false;
                                Log.Warning($" - Docs type not explicitly included: {docsType.Name}");
                            }
                        }

                        if (Configuration.ExcludedTypes.Count() > 0)
                        {
                            if (Configuration.ExcludedTypes.Contains(docsType.Name))
                            {
                                add = false;
                                Log.Warning($" - Docs type explicitly excluded: {docsType.Name}");
                            }
                        }

                        if (add)
                        {
                            Log.Success($" - Docs type included: {docsType.Name}");
                        }

                        break;
                    }
                }
                foreach (string excluded in Configuration.ExcludedAssemblies)
                {
                    if (docsType.AssemblyInfos.Count(x => x.AssemblyName.StartsWith(excluded)) > 0 || docsType.FullName.StartsWith(excluded))
                    {
                        add = false;
                        Log.Warning($"Docs xml file excluded: {fileInfo.FullName}");
                        break;
                    }
                }
            }

            int totalTypesAdded = 0;
            int totalMembersAdded = 0;
            if (add)
            {
                totalTypesAdded++;
                Types.Add(docsType);

                if (XmlHelper.TryGetChildElement(xDoc.Root, "Members", out XElement xeMembers))
                {
                    foreach (XElement xeMember in xeMembers.Elements("Member"))
                    {
                        DocsMember member = new DocsMember(fileInfo.FullName, docsType, xeMember);
                        totalMembersAdded++;
                        Members.Add(member);
                    }
                }

                if (totalTypesAdded > 0 || totalMembersAdded > 0)
                {
                    Log.Success($"{totalTypesAdded} type(s) added and {totalMembersAdded} member(s) added from file '{fileInfo.FullName}'");
                }
            }
        }

    }
}

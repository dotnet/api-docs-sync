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
        private Configuration Config { get; set; }

        private XDocument? xDoc = null;

        public readonly List<DocsType> Types = new List<DocsType>();
        public readonly List<DocsMember> Members = new List<DocsMember>();

        public DocsCommentsContainer(Configuration config)
        {
            Config = config;
        }

        public void CollectFiles()
        {
            foreach (FileInfo fileInfo in EnumerateFiles())
            {
                LoadFile(fileInfo);
            }
            Log.Line();
        }

        public void Save()
        {
            if (Config.Save)
            {
                List<string> savedFiles = new List<string>();
                foreach (var type in Types.Where(x => x.Changed))
                {
                    Log.Warning(false, $"Saving changes for {type.FilePath}:");

                    try
                    {
                        StreamReader sr = new StreamReader(type.FilePath);
                        int x = sr.Read(); // Force the first read to be done so the encoding is detected
                        Encoding encoding = sr.CurrentEncoding;
                        sr.Close();

                        // These settings prevent the addition of the <xml> element on the first line and will preserve indentation+endlines
                        XmlWriterSettings xws = new XmlWriterSettings
                        {
                            OmitXmlDeclaration = true,
                            Indent = true,
                            Encoding = encoding, //Encoding.GetEncoding("ISO-8859-1"),
                            CheckCharacters = false
                        };

                        using (XmlWriter xw = XmlWriter.Create(type.FilePath, xws))
                        {
                            type.XDoc.Save(xw);
                        }

                        // Workaround to delete the annoying endline added by XmlWriter.Save
                        string fileData = File.ReadAllText(type.FilePath);
                        if (!fileData.EndsWith(Environment.NewLine))
                        {
                            File.WriteAllText(type.FilePath, fileData + Environment.NewLine, encoding);
                        }

                        Log.Success(" [Saved]");
                    }
                    catch (Exception e)
                    {
                        Log.Error(e.Message);
                        Log.Line();
                        Log.Error(e.StackTrace ?? string.Empty);
                        if (e.InnerException != null)
                        {
                            Log.Line();
                            Log.Error(e.InnerException.Message);
                            Log.Line();
                            Log.Error(e.InnerException.StackTrace ?? string.Empty);
                        }
                        System.Threading.Thread.Sleep(1000);
                    }

                    Log.Line();
                }
            }
        }

        private bool HasAllowedName(FileInfo fileInfo)
        {
            return !fileInfo.Name.StartsWith("ns-") &&
                fileInfo.Name != "index.xml" &&
                fileInfo.Name != "_filter.xml";
        }

        private List<FileInfo> EnumerateFiles()
        {
            Log.Info("Looking for Docs xml files...");

            List<FileInfo> fileInfos = new List<FileInfo>();

            foreach (DirectoryInfo rootDir in Config.DirsDocsXml)
            {
                foreach (string included in Config.IncludedAssemblies)
                {
                    foreach (DirectoryInfo subDir in rootDir.EnumerateDirectories($"{included}*", SearchOption.TopDirectoryOnly))
                    {
                        if (!Configuration.ForbiddenDirectories.Contains(subDir.Name) && !subDir.Name.EndsWith(".Tests"))
                        {
                            foreach (FileInfo fileInfo in subDir.EnumerateFiles("*.xml", SearchOption.AllDirectories))
                            {
                                if (HasAllowedName(fileInfo))
                                {
                                    string nameWithoutExtension = fileInfo.Name.Replace(".xml", string.Empty);
                                    if (Config.IncludedTypes.Count > 0)
                                    {
                                        if (Config.IncludedTypes.Contains(nameWithoutExtension))
                                        {
                                            fileInfos.Add(fileInfo);
                                        }
                                    }
                                    else if (Config.ExcludedTypes.Count > 0)
                                    {
                                        if (!Config.ExcludedTypes.Contains(nameWithoutExtension))
                                        {
                                            fileInfos.Add(fileInfo);
                                        }
                                    }
                                    else
                                    {
                                        fileInfos.Add(fileInfo);
                                    }
                                }
                            }
                        }
                    }

                    if (!Config.SkipInterfaceImplementations)
                    {
                        // Find interfaces
                        foreach (DirectoryInfo subDir in rootDir.EnumerateDirectories("System*", SearchOption.AllDirectories))
                        {
                            if (!Configuration.ForbiddenDirectories.Contains(subDir.Name) &&
                                Config.ExcludedAssemblies.Count(excluded => subDir.Name.StartsWith(excluded)) == 0 &&
                                !subDir.Name.EndsWith(".Tests"))
                            {
                                foreach (FileInfo fileInfo in subDir.EnumerateFiles("I*.xml", SearchOption.AllDirectories))
                                {
                                    // Ensure including interface files that start with I and then an uppercase letter, and prevent including files like 'Int'
                                    if (fileInfo.Name[1] >= 'A' || fileInfo.Name[1] <= 'Z')
                                    {
                                        fileInfos.Add(fileInfo);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            Log.Success("Finished looking for Docs xml files");

            return fileInfos;
        }

        private bool IsAssemblyExcluded(DocsType docsType)
        {
            foreach (string excluded in Config.ExcludedAssemblies)
            {
                if (docsType.AssemblyInfos.Count(x => x.AssemblyName.StartsWith(excluded)) > 0 || docsType.FullName.StartsWith(excluded))
                {
                    return true;
                }
            }

            return false;
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

            // If it's an interface, add it if the user allowed it
            if (docsType.Name.StartsWith('I') && !Config.SkipInterfaceImplementations)
            {
                add = true;
            }
            // Otherwise, add the API only if it's part of the included assemblies
            // or included types and is not among the excluded types
            else if (!IsAssemblyExcluded(docsType))
            {
                foreach (string included in Config.IncludedAssemblies)
                {
                    if (docsType.AssemblyInfos.Count(x => x.AssemblyName.StartsWith(included)) > 0 ||
                        docsType.FullName.StartsWith(included))
                    {
                        add = true;

                        if (Config.IncludedTypes.Count() > 0)
                        {
                            if (!Config.IncludedTypes.Contains(docsType.Name))
                            {
                                add = false;
                            }
                        }

                        if (Config.ExcludedTypes.Count() > 0)
                        {
                            if (Config.ExcludedTypes.Contains(docsType.Name))
                            {
                                add = false;
                            }
                        }

                        break;
                    }
                }
                
            }

            if (add)
            {
                int totalMembersAdded = 0;
                Types.Add(docsType);

                if (XmlHelper.TryGetChildElement(xDoc.Root, "Members", out XElement? xeMembers) && xeMembers != null)
                {
                    foreach (XElement xeMember in xeMembers.Elements("Member"))
                    {
                        DocsMember member = new DocsMember(fileInfo.FullName, docsType, xeMember);
                        totalMembersAdded++;
                        Members.Add(member);
                    }
                }

                string message = $"Type {docsType.DocId} added with {totalMembersAdded} member(s) included.";
                if (docsType.Name.StartsWith('I'))
                {
                    Log.Magenta("[Interface] - " + message);
                }
                else if (totalMembersAdded == 0)
                {
                    Log.Warning(message);
                }
                else
                {
                    Log.Success(message);
                }
            }
        }
    }
}

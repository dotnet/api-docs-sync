using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

/*
The IntelliSense xml comments files for...
A) Libraries - They are saved in:
    /artifacts/bin/<Assembly>/net<Version>-<Configuration>/<Assembly>.xml
B) coreclr - They saved in:
    artifacts/bin/coreclr/<OS>.<Architecture>.<Configuration>/IL/System.Private.CoreLib.xml

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
namespace DocsPortingTool.Libraries.IntelliSenseXml
{
    internal class IntelliSenseXmlCommentsContainer
    {
        private Configuration Config { get; set; }

        private XDocument? xDoc = null;

        // The IntelliSense xml files do not separate types from members, like ECMA xml files do - Everything is a member.
        public Dictionary<string, IntelliSenseXmlMember> Members = new();

        public IntelliSenseXmlCommentsContainer(Configuration config)
        {
            Config = config;
        }

        public void CollectFiles()
        {
            Log.Info("Looking for IntelliSense xml files...");

            foreach (FileInfo fileInfo in EnumerateFiles())
            {
                LoadFile(fileInfo, printSuccess: true);
            }

            Log.Success("Finished looking for IntelliSense xml files.");
            Log.Line();
        }

        private IEnumerable<FileInfo> EnumerateFiles()
        {
            foreach (DirectoryInfo dirInfo in Config.DirsIntelliSense)
            {
                // 1) Find all the xml files inside all the subdirectories inside the IntelliSense xml directory
                foreach (DirectoryInfo subDir in dirInfo.EnumerateDirectories("*", SearchOption.TopDirectoryOnly))
                {
                    if (!Configuration.ForbiddenBinSubdirectories.Contains(subDir.Name) &&
                        !subDir.Name.EndsWith(".Tests") &&
                        !subDir.Name.StartsWith("microsoft.netcore.app.runtime."))
                    {
                        foreach (FileInfo fileInfo in subDir.EnumerateFiles("*.xml", SearchOption.AllDirectories))
                        {
                            yield return fileInfo;
                        }
                    }
                }

                // 2) Find all the xml files in the top directory
                foreach (FileInfo fileInfo in dirInfo.EnumerateFiles("*.xml", SearchOption.TopDirectoryOnly))
                {
                    yield return fileInfo;
                }
            }
        }

        private void LoadFile(FileInfo fileInfo, bool printSuccess)
        {
            try
            {
                xDoc = XDocument.Load(fileInfo.FullName);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to load '{fileInfo.FullName}'. {ex}");
                return;
            }

            if (!TryGetAssemblyName(xDoc, fileInfo.FullName, out string? assembly))
            {
                return;
            }

            int totalAdded = 0;
            if (XmlHelper.TryGetChildElement(xDoc.Root!, "members", out XElement? xeMembers) && xeMembers != null)
            {
                foreach (XElement xeMember in xeMembers.Elements("member"))
                {
                    IntelliSenseXmlMember member = new IntelliSenseXmlMember(xeMember, assembly);

                    if (Config.IncludedAssemblies.Any(included => member.Assembly.StartsWith(included)) &&
                        !Config.ExcludedAssemblies.Any(excluded => member.Assembly.StartsWith(excluded)))
                    {
                        // No namespaces provided by the user means they want to port everything from that assembly
                        if (!Config.IncludedNamespaces.Any() ||
                                (Config.IncludedNamespaces.Any(included => member.Namespace.StartsWith(included)) &&
                                !Config.ExcludedNamespaces.Any(excluded => member.Namespace.StartsWith(excluded))))
                        {
                            totalAdded++;
                            Members.TryAdd(member.Name, member); // is it OK this encounters duplicates?
                        }
                    }
                }
            }

            if (printSuccess && totalAdded > 0)
            {
                Log.Success($"{totalAdded} IntelliSense xml member(s) added from xml file '{fileInfo.FullName}'");
            }
        }

        // Verifies the file is properly formed while attempting to retrieve the assembly name.
        private bool TryGetAssemblyName(XDocument? xDoc, string fileName, [NotNullWhen(returnValue: true)] out string? assembly)
        {
            assembly = null;

            if (xDoc == null)
            {
                Log.Error($"The XDocument was null: {fileName}");
                return false;
            }

            if (xDoc.Root == null)
            {
                Log.Error($"The IntelliSense xml file does not contain a root element: {fileName}");
                return false;
            }

            if (xDoc.Root.Name == "linker" || xDoc.Root.Name == "FileList")
            {
                // This is a linker suppression file or a framework list
                return false;
            }

            if (xDoc.Root.Name != "doc")
            {
                Log.Error($"The IntelliSense xml file does not contain a doc element: {fileName}");
                return false;
            }

            if (!xDoc.Root.HasElements)
            {
                Log.Error($"The IntelliSense xml file doc element not have any children: {fileName}");
                return false;
            }

            if (xDoc.Root.Elements("assembly").Count() != 1)
            {
                Log.Error($"The IntelliSense xml file does not contain exactly 1 'assembly' element: {fileName}");
                return false;
            }

            if (xDoc.Root.Elements("members").Count() != 1)
            {
                Log.Error($"The IntelliSense xml file does not contain exactly 1 'members' element: {fileName}");
                return false;
            }

            XElement? xAssembly = xDoc.Root.Element("assembly");
            if (xAssembly == null)
            {
                Log.Error($"The assembly xElement is null: {fileName}");
                return false;
            }
            if (xAssembly.Elements("name").Count() != 1)
            {
                Log.Error($"The IntelliSense xml file assembly element does not contain exactly 1 'name' element: {fileName}");
                return false;
            }

            assembly = xAssembly.Element("name")!.Value;
            if (string.IsNullOrEmpty(assembly))
            {
                Log.Error($"The IntelliSense xml file assembly string is null or empty: {fileName}");
                return false;
            }

            // The System.Private.CoreLib xml file should be mapped to the System.Runtime assembly
            if (assembly.ToUpperInvariant() == "SYSTEM.PRIVATE.CORELIB")
            {
                assembly = "System.Runtime";
            }

            return true;
        }
    }
}

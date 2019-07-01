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
    class TripleSlashCommentsContainer
    {
        private static readonly string[] ForbiddenDirectories = new[] { "binplacePackages", "docs", "mscorlib", "native", "netfx", "netstandard", "pkg", "Product", "ref", "runtime", "shimsTargetRuntime", "tests", "winrt" };

        private XDocument xDoc = null;

        public List<TripleSlashAssembly> Assemblies = new List<TripleSlashAssembly>();

        public TripleSlashCommentsContainer()
        {
        }

        public void Load()
        {
            Log.Info("Loading triple slash xml files...");
            foreach (DirectoryInfo tripleSlashDir in CLArgumentVerifier.PathsTripleSlashXmls)
            {
                foreach (DirectoryInfo subDir in tripleSlashDir.EnumerateDirectories("*", SearchOption.TopDirectoryOnly))
                {
                    // Find all the xml files inside the subdirectories within the triple slash xml directory
                    if (!ForbiddenDirectories.Contains(subDir.Name) && !subDir.Name.EndsWith(".Tests"))
                    {
                        foreach (FileInfo fileInfo in subDir.EnumerateFiles("*.xml", SearchOption.AllDirectories))
                        {
                            LoadFile(fileInfo);
                        }
                    }
                }

                foreach (FileInfo fileInfo in tripleSlashDir.EnumerateFiles("*.xml", SearchOption.TopDirectoryOnly))
                {
                    // Now analyze the xml files directly inside the triple slash xml directory
                    LoadFile(fileInfo);
                }
            }
            Log.Info("Finished loading triple slash xml files!");
        }

        private void LoadFile(FileInfo fileInfo)
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
                Log.Error("Tripls slash xml file does not contain a doc/assembly element: {0}", fileInfo.FullName);
                return;
            }

            if (xDoc.Root.Elements("members").Count() != 1)
            {
                Log.Error("Triple slash xml file does not contain a doc/members element: {0}", fileInfo.FullName);
                return;
            }

            TripleSlashAssembly assembly = new TripleSlashAssembly(fileInfo.FullName, xDoc.Root);

            bool add = false;
            foreach (string included in CLArgumentVerifier.IncludedAssemblies)
            {
                if (assembly.Name.StartsWith(included))
                {
                    add = true;
                    break;
                }
            }

            foreach (string excluded in CLArgumentVerifier.ExcludedAssemblies)
            {
                if (assembly.Name.StartsWith(excluded))
                {
                    add = false;
                    Log.Warning("Triple slash xml file excluded: {0}", fileInfo.FullName);
                    break;
                }
            }

            if (add)
            {
                Assemblies.Add(assembly);
                Log.Success("Triple slash xml file included: {0}", fileInfo.FullName);
            }
        }
    }
}

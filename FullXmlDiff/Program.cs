using DocsPortingTool.TripleSlash;
using Shared;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace FullXmlDiff
{
    class Program
    {
        static void Main(string[] args)
        {
            //CLArgumentVerifier.Verify(args);

            //List<FileInfo> filesLeft  = CollectFiles(CLArgumentVerifier.LeftDirectories, "left");
            //List<FileInfo> filesRight = CollectFiles(CLArgumentVerifier.RightDirectories, "right");

            //List<TripleSlashAssembly> assembliesLeft  = CollectAssemblies(filesLeft, "left");
            //List<TripleSlashAssembly> assembliesRight = CollectAssemblies(filesRight, "right");

            //CompareAssemblies(assembliesLeft, assembliesRight);
        }

        //private static List<FileInfo> CollectFiles(DirectoryInfo[] directories, string side)
        //{
        //    if (CLArgumentVerifier.Verbose)
        //        Log.Working($"Collecting {side} files...");

        //    List<FileInfo> files = new List<FileInfo>();

        //    foreach (DirectoryInfo directory in directories)
        //    {
        //        if (CLArgumentVerifier.Verbose)
        //            Log.Working($"    - Visiting directory '{directory.FullName}'...");

        //        foreach (FileInfo file in directory.EnumerateFiles("*.xml", SearchOption.TopDirectoryOnly))
        //        {
        //            if (files.Count(x => x.FullName == file.FullName) == 0)
        //            {
        //                files.Add(file);
        //                if (CLArgumentVerifier.Verbose)
        //                    Log.Success($"        - Added file '{file.FullName}'");
        //            }
        //            else
        //            {
        //                if (CLArgumentVerifier.Verbose)
        //                    Log.Warning($"        - Skipped adding existing file '{file.FullName}'");
        //            }
        //        }
        //    }

        //    return files;
        //}

        //private static List<TripleSlashAssembly> CollectAssemblies(List<FileInfo> files, string side)
        //{
        //    if (CLArgumentVerifier.Verbose)
        //        Log.Working($"Collecting {side} assemblies...");

        //    List<TripleSlashAssembly> assemblies = new List<TripleSlashAssembly>();

        //    foreach (FileInfo file in files)
        //    {
        //        XDocument xDoc = XDocument.Load(file.FullName);
        //        TripleSlashAssembly assembly = new TripleSlashAssembly(file.FullName, xDoc.Root);
        //        if (assemblies.Count(x => x.Name == assembly.Name) == 0)
        //        {
        //            assemblies.Add(assembly);
        //        }
        //    }

        //    return assemblies;
        //}

        //private static void CompareAssemblies(List<TripleSlashAssembly> assembliesLeft, List<TripleSlashAssembly> assembliesRight)
        //{
        //    if (assembliesLeft.Count < assembliesRight.Count)
        //    {
        //        Log.Success(false, "Total right assemblies are a superset of left!");
        //    }
        //    else if (assembliesLeft.Count > assembliesRight.Count)
        //    {
        //        Log.Error(false, "Total right assemblies are NOT a superset of left!");
        //    }
        //    else
        //    {
        //        Log.Success(false, "Total assemblies match on left and right!");
        //    }
        //    Log.Info($" Left: {assembliesLeft.Count}, Right {assembliesRight.Count}");
        //    Log.Line();

        //    foreach (TripleSlashAssembly leftAssembly in assembliesLeft)
        //    {
        //        if (CLArgumentVerifier.Verbose)
        //            Log.Info($"Comparing assembly '{leftAssembly.Name}'...");

        //        bool assemblyExistsRight = false;
        //        foreach (TripleSlashAssembly rightAssembly in assembliesRight.Where(x => x.Name == leftAssembly.Name))
        //        {
        //            if (assemblyExistsRight)
        //            {
        //                Log.Error($"        Assembly '{leftAssembly.Name}' was found more than once on right!");
        //                break;
        //            }

        //            assemblyExistsRight = true;

        //            bool printMessage = false;
        //            if (leftAssembly.Members.Count < rightAssembly.Members.Count)
        //            {
        //                if (CLArgumentVerifier.Verbose)
        //                {
        //                    printMessage = true;
        //                    Log.Warning(false, $"    Members in right '{leftAssembly.Name}' are a superset of left!");
        //                }
        //            }
        //            else if (leftAssembly.Members.Count > rightAssembly.Members.Count)
        //            {
        //                printMessage = true;
        //                Log.Error(false, $"    Members in right '{leftAssembly.Name}' are NOT a superset of left!");
        //            }
        //            else
        //            {
        //                if (CLArgumentVerifier.Verbose)
        //                {
        //                    printMessage = true;
        //                    Log.Success(false, $"    Total members in '{leftAssembly.Name}' match!");
        //                }
        //            }
        //            if (printMessage)
        //            {
        //                Log.Info($" Left: {leftAssembly.Members.Count}, Right {rightAssembly.Members.Count}");
        //            }
        //        }

        //        if (!assemblyExistsRight)
        //        {
        //            Log.Error($"        Assembly '{leftAssembly.Name}' was not found on the right side!");
        //        }
        //    }
        //}
    }
}

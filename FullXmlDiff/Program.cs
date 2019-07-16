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
            CLArgumentVerifier.Verify(args);

            List<FileInfo> filesLeft = CollectFiles(CLArgumentVerifier.LeftDirectories, "left");
            List<FileInfo> filesRight = CollectFiles(CLArgumentVerifier.RightDirectories, "right");

            List<FileInfo> missingOnRight = GetFileDiff(filesLeft, filesRight);

            TripleSlashCommentsContainer containerLeft = CollectMembers(filesLeft, "left");
            TripleSlashCommentsContainer containerRight = CollectMembers(filesRight, "right");

            CompareMembers(containerLeft, containerRight);
        }

        private static List<FileInfo> GetFileDiff(List<FileInfo> filesLeft, List<FileInfo> filesRight)
        {
            Log.Working("Looking for files that are on the left side but not on the right side...");
            Log.Line();

            List<FileInfo> missingOnRight = new List<FileInfo>();
            foreach (FileInfo leftFile in filesLeft)
            {
                if (filesRight.Count(x => x.Name == leftFile.Name) == 0)
                {
                    Log.Error($"    {leftFile.Name}");
                    missingOnRight.Add(leftFile);
                }
            }

            Log.Line();

            return missingOnRight;
        }

        private static List<FileInfo> CollectFiles(DirectoryInfo[] directories, string side)
        {
            Log.Working($"Collecting {side} files...");

            List<FileInfo> files = new List<FileInfo>();

            foreach (DirectoryInfo directory in directories)
            {
                Log.Working($"    - Visiting directory '{directory.FullName}'...");

                foreach (FileInfo file in directory.EnumerateFiles("*.xml", SearchOption.TopDirectoryOnly))
                {
                    if (files.Count(x => x.FullName == file.FullName) == 0)
                    {
                        files.Add(file);
                    }
                    else
                    {
                        Log.Warning($"          - Skipped adding existing file '{file.FullName}'");
                    }
                }
            }

            return files;
        }

        private static TripleSlashCommentsContainer CollectMembers(List<FileInfo> fileInfos, string side)
        {
            Log.Working($"Collecting {side} members...");
            Log.Line();

            TripleSlashCommentsContainer container = new TripleSlashCommentsContainer();

            foreach (FileInfo fileInfo in fileInfos)
            {
                container.LoadFile(fileInfo, new List<string> { "System", "Microsoft", "Windows" }, new List<string>(), printSuccess: false);
            }

            Log.Success($"    Total {side} members: {container.Members.Count}");
            Log.Line();

            return container;
        }

        private static void CompareMembers(TripleSlashCommentsContainer containerLeft, TripleSlashCommentsContainer containerRight)
        {
            Log.Working("Comparing members...");
            Log.Line();

            Log.Info($"Left: {containerLeft.Members.Count}, Right {containerRight.Members.Count}");
            if (containerLeft.Members.Count < containerRight.Members.Count)
            {
                Log.Success(false, "    Total right assemblies are a superset of left!");
            }
            else if (containerLeft.Members.Count > containerRight.Members.Count)
            {
                Log.Error(false, "    Total right assemblies are NOT a superset of left!");
            }
            else
            {
                Log.Success(false, "    Total assemblies match on left and right!");
            }
            Log.Line();

            Log.Working("Looking for left members that are not on the right...");
            foreach (TripleSlashMember leftMember in containerLeft.Members)
            {
                if (containerRight.Members.Count(x => x.Name == leftMember.Name) == 0)
                {
                    Log.Error("    " + leftMember.Name.Replace("{", "{{").Replace("}", "}}"));
                }
            }

            Log.Line();
        }
    }
}

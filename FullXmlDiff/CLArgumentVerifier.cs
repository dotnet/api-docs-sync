using Shared;
using System;
using System.IO;
using System.Linq;

namespace FullXmlDiff
{
    public class CLArgumentVerifier
    {
        private enum Mode
        {
            Initial,
            Left,
            Right
        }

        public static DirectoryInfo[] LeftDirectories { get; private set; }

        public static DirectoryInfo[] RightDirectories { get; private set; }

        public static void Verify(string[] args)
        {
            if (args.Length == 0)
            {
                Log.LogErrorAndExit("No arguments passed.");
            }

            Mode mode = Mode.Initial;
            foreach (string arg in args)
            {
                switch (mode)
                {
                    case Mode.Initial:
                        {
                            switch (arg.ToLowerInvariant())
                            {
                                case "-left":
                                    {
                                        mode = Mode.Left;
                                        break;
                                    }
                                case "-right":
                                    {
                                        mode = Mode.Right;
                                        break;
                                    }
                                default:
                                    {
                                        throw new ArgumentException($"Unexpected argument: {arg}");
                                    }
                            }
                            break;
                        }

                    case Mode.Left:
                        {
                            LeftDirectories = TryGetDirectoryInfos(arg);
                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.Right:
                        {
                            RightDirectories = TryGetDirectoryInfos(arg);
                            mode = Mode.Initial;
                            break;
                        }

                    default:
                        throw new ArgumentOutOfRangeException($"Unrecognized mode: {mode}");
                }
            }

            if (mode != Mode.Initial)
            {
                Log.LogErrorPrintHelpAndExit(PrintHelp, "A parameter was incomplete.");
            }

            Log.Info("Directories on left side:");
            foreach (DirectoryInfo dir in LeftDirectories)
            {
                Log.Working($"    - {dir.FullName}");
            }

            Log.Info("Directories on right side:");
            foreach (DirectoryInfo dir in RightDirectories)
            {
                Log.Working($"    - {dir.FullName}");
            }
            Log.Line();
        }

        private static DirectoryInfo[] TryGetDirectoryInfos(string arg)
        {
            if (string.IsNullOrWhiteSpace(arg))
            {
                Log.LogErrorAndExit("The path was empty.");
            }
            string[] paths = arg.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (paths.Length == 0)
            {
                Log.LogErrorAndExit("You did not pass any paths.");
            }
            foreach (string path in paths)
            {
                if (!Directory.Exists(path))
                {
                    Log.LogErrorAndExit($"This directory does not exist: {path}");
                }
                else if (Directory.EnumerateFiles(path, "*.xml", SearchOption.TopDirectoryOnly).Count() == 0)
                {
                    Log.LogErrorAndExit($"This directory does not contain any xml files: {path}");
                }
            }

            return (from path in paths select new DirectoryInfo(path)).ToArray();
        }

        private static void PrintHelp()
        {
            Log.Info(@"
                -left <path>[,<path2>,...,<pathN>]          List of comma-separated full paths to folders containing xml files with triple slash comments.
                                                            This is the list of OLD files (the subset).

                -right <path>[,<path2>,...,<pathN>]         List of comma-separated full paths to folders containing xml files with triple slash comments.
                                                            This is the list of NEW files (the superset).

                -verbose                                    If present, prints all messages.
                                                            If absent, will only print the error messages.
                                                            No arguments required.
            ");
        }
    }
}
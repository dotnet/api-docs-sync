using Shared;
using System;
using System.Collections.Generic;
using System.IO;

namespace DocsPortingTool
{
    public static class CLArgumentVerifier
    {
        #region Private members

        private static readonly char Separator = ',';

        private enum Mode
        {
            Include,
            Initial,
            Docs,
            Exclude,
            Save,
            TripleSlash
        }

        #endregion

        #region Public members

        public static readonly string[] AllowedAssemblyPrefixes = new string[] { "System", "Microsoft", "Windows" };

        public static readonly List<DirectoryInfo> PathsTripleSlashXmls = new List<DirectoryInfo>();
        public static DirectoryInfo PathDocsXml { get; private set; }

        public static readonly List<string> IncludedAssemblies = new List<string>();
        public static readonly List<string> ExcludedAssemblies = new List<string>();

        public static bool Save { get; private set; }

        #endregion

        #region Public methods

        public static void Verify(string[] args)
        {
            Mode mode = Mode.Initial;

            Log.Info("Verifying CLI arguments...");

            if (args == null || args.Length == 0)
            {
                Log.LogErrorPrintHelpAndExit(PrintHelp, "No arguments passed to the executable.");
            }

            foreach(string arg in args)
            {
                switch (mode)
                {
                    case Mode.Include:
                        {
                            string[] splittedArg = arg.Split(Separator, StringSplitOptions.RemoveEmptyEntries);

                            if (splittedArg.Length > 0)
                            {
                                Log.Print(true, ConsoleColor.Cyan, "Included assemblies:");
                                foreach (string assembly in splittedArg)
                                {
                                    IncludedAssemblies.Add(assembly);
                                    Log.Info($"  -  {assembly}");
                                }
                            }
                            else
                            {
                                Log.LogErrorPrintHelpAndExit(PrintHelp, "You must specify at least one assembly.");
                            }

                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.Docs:
                        {
                            PathDocsXml = new DirectoryInfo(Path.Combine(arg, "xml"));

                            if (!PathDocsXml.Exists)
                            {
                                Log.LogErrorAndExit(string.Format("The dotnet-api-docs/xml folder does not exist: {0}", PathDocsXml));
                            }

                            Log.Print(true, ConsoleColor.Cyan, $"Specified dotnet-api-docs/xml location:");
                            Log.Info($"  -  {PathDocsXml}");

                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.Exclude:
                        {
                            string[] splittedArg = arg.Split(Separator, StringSplitOptions.RemoveEmptyEntries);

                            if (splittedArg.Length > 0)
                            {
                                Log.Print(true, ConsoleColor.Cyan, "Excluded assemblies:");
                                foreach (string assembly in splittedArg)
                                {
                                    ExcludedAssemblies.Add(assembly);
                                    Log.Info($"  -  {assembly}");
                                }
                            }
                            else
                            {
                                Log.LogErrorPrintHelpAndExit(PrintHelp, "You must specify at least one assembly.");
                            }

                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.Initial:
                        {
                            switch (arg.ToLower())
                            {
                                case "-h":
                                case "-help":
                                    PrintHelp();
                                    Environment.Exit(0);
                                    break;

                                case "-docs":
                                    mode = Mode.Docs;
                                    break;

                                case "-exclude":
                                    mode = Mode.Exclude;
                                    break;

                                case "-include":
                                    mode = Mode.Include;
                                    break;

                                case "-save":
                                    mode = Mode.Save;
                                    break;

                                case "-tripleslash":
                                    mode = Mode.TripleSlash;
                                    break;

                                default:
                                    Log.LogErrorPrintHelpAndExit(PrintHelp, string.Format("Unrecognized argument '{0}'.", arg));
                                    break;
                            }
                            break;
                        }

                    case Mode.Save:
                        {
                            if (!bool.TryParse(arg, out bool save))
                            {
                                Log.LogErrorAndExit("Invalid boolean value for the save argument: {0}", arg);
                            }

                            Save = save;

                            Log.Print(true, ConsoleColor.Cyan, "Save:");
                            Log.Info($"  -  {Save}");

                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.TripleSlash:
                        {
                            string[] splittedArg = arg.Split(Separator, StringSplitOptions.RemoveEmptyEntries);

                            if (splittedArg.Length > 0)
                            {
                                Log.Print(true, ConsoleColor.Cyan, "Triple slash locations:");
                                foreach (string dirPath in splittedArg)
                                {
                                    DirectoryInfo dirInfo = new DirectoryInfo(dirPath);
                                    if (!dirInfo.Exists)
                                    {
                                        Log.LogErrorAndExit(string.Format("The triple slash xml directory does not exist: {0}", dirPath));
                                    }
                                    PathsTripleSlashXmls.Add(dirInfo);
                                    Log.Info($"  -  {dirInfo.FullName}");
                                }
                            }
                            else
                            {
                                Log.LogErrorPrintHelpAndExit(PrintHelp, "You must specify at least one path containing triple slash xml files.");
                            }

                            mode = Mode.Initial;
                            break;
                        }

                    default:
                        {
                            Log.LogErrorPrintHelpAndExit(PrintHelp, "Unexpected mode.");
                            break;
                        }
                }
            }

            if (mode != Mode.Initial)
            {
                Log.LogErrorPrintHelpAndExit(PrintHelp, "You missed an argument value.");
            }

            if (PathDocsXml == null)
            {
                Log.LogErrorPrintHelpAndExit(PrintHelp, "You must specify a path to the dotnet-api-docs xml folder with -docs.");
            }

            if (PathsTripleSlashXmls.Count == 0)
            {
                Log.LogErrorPrintHelpAndExit(PrintHelp, "You must specify at least one triple slash xml folder path with -tripleslash.");
            }

            if (IncludedAssemblies.Count == 0)
            {
                Log.LogErrorPrintHelpAndExit(PrintHelp, "You must specify at least one assembly with -include.");
            }
        }

        public static bool HasAllowedAssemblyPrefix(string pathName)
        {
            foreach (string prefix in AllowedAssemblyPrefixes)
            {
                if (pathName.StartsWith(prefix))
                {
                    return true;
                }
            }

            return false;
        }

        public static void PrintHelp()
        {
            Log.Print(true, ConsoleColor.Cyan, @"
This tool finds and ports triple slash comments found in .NET repos but do not yet exist in the dotnet-api-docs repo.

Options:

    no arguments:   -h or -help             Optional. Displays this help message. If used, nothing else will be processed.


    folder path:    -docs                   Mandatory. The absolute directory path to the Docs repo.

                                                Usage example:
                                                    -docs %SourceRepos%\dotnet-api-docs


    string list:    -exclude                Optional. Comma separated list (no spaces) of specific .NET assemblies to ignore. Default is empty.
                                                Usage example:
                                                    -exclude System.IO.Compression,System.IO.Pipes


    string:         -include                Mandatory. Comma separated list (no spaces) of assemblies to include.

                                                Usage example:
                                                    System.IO,System.Runtime.Intrinsics


    bool:           -save                   Optional. Wether we want to save the changes in the dotnet-api-docs xml files. Default is false.
                                                Usage example:
                                                    -save true


    folder paths:   -tripleslash            Mandatory. List of absolute directory paths (comma separated) where we should look for triple slash comment xml files.

                                                Known locations:
                                                    > CoreCLR:   coreclr\bin\Product\Windows_NT.x64.Debug\IL\
                                                    > CoreFX:    corefx\artifacts\bin\
                                                    > WinForms:  winforms\artifacts\bin\
                                                    > WPF:       wpf\.tools\native\bin\dotnet-api-docs_netcoreapp3.0\0.0.0.1\_intellisense\\netcore-3.0\

                                                Usage example:
                                                    -tripleslash %SourceRepos%\corefx\artifacts\bin\,%SourceRepos%\coreclr\bin\Product\Windows_NT.x64.Debug\IL\

            ");
        }

        #endregion
    }
}
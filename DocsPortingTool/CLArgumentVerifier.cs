using Shared;
using System;
using System.Collections.Generic;
using System.Dynamic;
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
            PrintUndoc,
            Save,
            TripleSlash
        }

        #endregion

        #region Public members

        public static readonly string[] AllowedAssemblyPrefixes = new string[] { "System", "Microsoft", "Windows" };
        public static readonly string[] ForbiddenDirectories = new[] { "binplacePackages", "docs", "mscorlib", "native", "netfx", "netstandard", "pkg", "Product", "ref", "runtime", "shimsTargetRuntime", "tests", "winrt" };

        public static readonly List<DirectoryInfo> DirsTripleSlashXmls = new List<DirectoryInfo>();

        public static DirectoryInfo DirDocsXml { get; private set; }

        public static readonly List<string> IncludedAssemblies = new List<string>();
        public static readonly List<string> ExcludedAssemblies = new List<string>();

        public static bool Save { get; private set; }
        public static bool PrintUndoc { get; private set; }

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
                                Log.Working("Included assemblies:");
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
                            DirDocsXml = new DirectoryInfo(arg);

                            if (!DirDocsXml.Exists)
                            {
                                Log.LogErrorAndExit(string.Format("The documentation folder does not exist: {0}", DirDocsXml));
                            }

                            Log.Working($"Specified documentation location:");
                            Log.Info($"  -  {DirDocsXml}");

                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.Exclude:
                        {
                            string[] splittedArg = arg.Split(Separator, StringSplitOptions.RemoveEmptyEntries);

                            if (splittedArg.Length > 0)
                            {
                                Log.Working("Excluded assemblies:");
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
                            switch (arg.ToLowerInvariant())
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

                                //case "-dlls":
                                //    mode = Mode.DLLs;
                                //    break;

                                default:
                                    Log.LogErrorPrintHelpAndExit(PrintHelp, string.Format("Unrecognized argument '{0}'.", arg));
                                    break;
                            }
                            break;
                        }

                    case Mode.PrintUndoc:
                        {
                            if (!bool.TryParse(arg, out bool printUndoc))
                            {
                                Log.LogErrorAndExit("Invalid boolean value for the printundoc argument: {0}", arg);
                            }

                            PrintUndoc = printUndoc;

                            Log.Working("Print undocumented:");
                            Log.Info($"  -  {PrintUndoc}");

                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.Save:
                        {
                            if (!bool.TryParse(arg, out bool save))
                            {
                                Log.LogErrorAndExit("Invalid boolean value for the save argument: {0}", arg);
                            }

                            Save = save;

                            Log.Working("Save:");
                            Log.Info($"  -  {Save}");

                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.TripleSlash:
                        {
                            string[] splittedDirPaths = arg.Split(',', StringSplitOptions.RemoveEmptyEntries);

                            Log.Working($"Specified triple slash locations:");
                            foreach (string dirPath in splittedDirPaths)
                            {
                                DirectoryInfo dirInfo = new DirectoryInfo(dirPath);
                                if (!dirInfo.Exists)
                                {
                                    Log.LogErrorAndExit(string.Format("This triple slash xml directory does not exist: {0}", dirPath));
                                }

                                DirsTripleSlashXmls.Add(dirInfo);
                                Log.Info($"  -  {dirPath}");
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

            if (DirDocsXml == null)
            {
                Log.LogErrorPrintHelpAndExit(PrintHelp, "You must specify a path to the dotnet-api-docs xml folder with -docs.");
            }

            if (DirsTripleSlashXmls.Count == 0)
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
            Log.Working(@"
This tool finds and ports triple slash comments found in .NET repos but do not yet exist in the dotnet-api-docs repo.

Change %SourceRepos% to match the location of all your cloned git repos.

Options:

    no arguments:   -h or -help             Optional. Displays this help message. If used, nothing else will be processed.



    folder path:    -docs                   Mandatory. The absolute directory path where your documentation xml files are located.

                                                Known locations:
                                                    > CoreFX and CoreCLR: %SourceRepos%\dotnet-api-docs\xml
                                                    > WPF:                ? (TODO)
                                                    > WinForms:           ? (TODO)

                                                Usage example:
                                                    -docs %SourceRepos%\dotnet-api-docs\xml



    string list:    -exclude                Optional. Comma separated list (no spaces) of specific .NET assemblies to ignore. Default is empty.

                                                Usage example:
                                                    -exclude System.IO.Compression,System.IO.Pipes



    string:         -include                Mandatory. Comma separated list (no spaces) of assemblies to include.

                                                Usage example:
                                                    System.IO,System.Runtime.Intrinsics



    boo:            -printundoc             Optional. Will print a detailed summary of all the docs APIs that are undocumented. Default is false.

                                                Usage example:
                                                    -printundoc true



    bool:           -save                   Optional. Wether we want to save the changes in the dotnet-api-docs xml files. Default is false.

                                                Usage example:
                                                    -save true



    folder path:   -tripleslash             Mandatory. A comma separated list (no spaces) of absolute directory paths where we should recursively look for triple slash comment xml files.

                                                Known locations:
                                                    > CoreCLR:   %SourceRepos%\coreclr\bin\Product\Windows_NT.x64.Debug\IL\
                                                    > CoreFX:    %SourceRepos%\corefx\artifacts\bin\
                                                    > WinForms:  %SourceRepos%\winforms\artifacts\bin\
                                                    > WPF:       %SourceRepos%\wpf\.tools\native\bin\dotnet-api-docs_netcoreapp3.0\0.0.0.1\_intellisense\\netcore-3.0\

                                                Usage example:
                                                    -tripleslash %SourceRepos%\corefx\artifacts\bin\

            ");
        }

        #endregion
    }
}
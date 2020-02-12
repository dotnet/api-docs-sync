using Shared;
using System;
using System.IO;
using System.Xml.Linq;

namespace DocsPortingTool
{
    public static class CLArgumentVerifier
    {
        #region Private members

        private static readonly char Separator = ',';

        private enum Mode
        {
            Docs,
            ExcludedAssemblies,
            ExcludedTypes,
            IncludedAssemblies,
            IncludedTypes,
            Initial,
            PrintUndoc,
            Save,
            SkipExceptions,
            TripleSlash
        }

        #endregion

        #region Public methods

        public static void GetConfiguration(string[] args)
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
                    case Mode.Docs:
                        {
                            Configuration.DirDocsXml = new DirectoryInfo(arg);

                            if (!Configuration.DirDocsXml.Exists)
                            {
                                Log.LogErrorAndExit(string.Format("The documentation folder does not exist: {0}", Configuration.DirDocsXml));
                            }

                            Log.Working($"Specified documentation location:");
                            Log.Info($"  -  {Configuration.DirDocsXml}");

                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.ExcludedAssemblies:
                        {
                            string[] splittedArg = arg.Split(Separator, StringSplitOptions.RemoveEmptyEntries);

                            if (splittedArg.Length > 0)
                            {
                                Log.Working("Excluded assemblies:");
                                foreach (string assembly in splittedArg)
                                {
                                    Log.Working($" - {assembly}");
                                    Configuration.ExcludedAssemblies.Add(assembly);
                                }
                            }
                            else
                            {
                                Log.LogErrorPrintHelpAndExit(PrintHelp, "You must specify at least one assembly.");
                            }

                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.ExcludedTypes:
                        {
                            string[] splittedArg = arg.Split(Separator, StringSplitOptions.RemoveEmptyEntries);

                            if (splittedArg.Length > 0)
                            {
                                Log.Working($"Excluded types:");
                                foreach (string typeName in splittedArg)
                                {
                                    Log.Working($" - {typeName}");
                                    Configuration.ExcludedTypes.Add(typeName);
                                }
                            }
                            else
                            {
                                Log.LogErrorPrintHelpAndExit(PrintHelp, "You must specify at least one type name.");
                            }

                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.IncludedAssemblies:
                        {
                            string[] splittedArg = arg.Split(Separator, StringSplitOptions.RemoveEmptyEntries);

                            if (splittedArg.Length > 0)
                            {
                                Log.Working($"Included assemblies:");
                                foreach (string assembly in splittedArg)
                                {
                                    Log.Working($" - {assembly}");
                                    Configuration.IncludedAssemblies.Add(assembly);
                                }
                            }
                            else
                            {
                                Log.LogErrorPrintHelpAndExit(PrintHelp, "You must specify at least one assembly.");
                            }

                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.IncludedTypes:
                        {
                            string[] splittedArg = arg.Split(Separator, StringSplitOptions.RemoveEmptyEntries);

                            if (splittedArg.Length > 0)
                            {
                                Log.Working($"Included types:");
                                foreach (string typeName in splittedArg)
                                {
                                    Log.Working($" - {typeName}");
                                    Configuration.IncludedTypes.Add(typeName);
                                }
                            }
                            else
                            {
                                Log.LogErrorPrintHelpAndExit(PrintHelp, "You must specify at least one type name.");
                            }

                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.Initial:
                        {
                            switch (arg.ToLowerInvariant())
                            {
                                case "-docs":
                                    mode = Mode.Docs;
                                    break;

                                case "-excludedassemblies":
                                    mode = Mode.ExcludedAssemblies;
                                    break;

                                case "-excludedtypes":
                                    mode = Mode.ExcludedTypes;
                                    break;

                                case "-h":
                                case "-help":
                                    PrintHelp();
                                    Environment.Exit(0);
                                    break;

                                case "-includedassemblies":
                                    mode = Mode.IncludedAssemblies;
                                    break;

                                case "-includedtypes":
                                    mode = Mode.IncludedTypes;
                                    break;

                                case "-printundoc":
                                    mode = Mode.PrintUndoc;
                                    break;

                                case "-save":
                                    mode = Mode.Save;
                                    break;

                                case "-skipexceptions":
                                    mode = Mode.SkipExceptions;
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

                    case Mode.PrintUndoc:
                        {
                            if (!bool.TryParse(arg, out bool printUndoc))
                            {
                                Log.LogErrorAndExit("Invalid boolean value for the printundoc argument: {0}", arg);
                            }

                            Configuration.PrintUndoc = printUndoc;

                            Log.Working("Print undocumented:");
                            Log.Info($"  -  {Configuration.PrintUndoc}");

                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.Save:
                        {
                            if (!bool.TryParse(arg, out bool save))
                            {
                                Log.LogErrorAndExit($"Invalid boolean value for the save argument: {arg}");
                            }

                            Configuration.Save = save;

                            Log.Working("Save:");
                            Log.Info($"  -  {Configuration.Save}");

                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.SkipExceptions:
                        {
                            if (!bool.TryParse(arg, out bool skipExceptions))
                            {
                                Log.LogErrorAndExit($"Invalid boolean value for the skipExceptions argument: {arg}");
                            }

                            Configuration.SkipExceptions = skipExceptions;

                            Log.Working("Skip exceptions:");
                            Log.Info($"  -  {Configuration.SkipExceptions}");

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

                                Configuration.DirsTripleSlashXmls.Add(dirInfo);
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

            if (Configuration.DirDocsXml == null)
            {
                Log.LogErrorPrintHelpAndExit(PrintHelp, "You must specify a path to the dotnet-api-docs xml folder with -docs.");
            }

            if (Configuration.DirsTripleSlashXmls.Count == 0)
            {
                Log.LogErrorPrintHelpAndExit(PrintHelp, "You must specify at least one triple slash xml folder path with -tripleslash.");
            }

            if (Configuration.IncludedAssemblies.Count == 0)
            {
                Log.LogErrorPrintHelpAndExit(PrintHelp, "You must specify at least one assembly with -include.");
            }
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



    string list:    -excludedassemblies         Optional. Comma separated list (no spaces) of specific .NET assemblies to ignore. Default is empty.

                                                Usage example:
                                                    -excludedassemblies System.IO.Compression,System.IO.Pipes



    string list:    -includedassemblies         Mandatory. Comma separated list (no spaces) of assemblies to include.

                                                Usage example:
                                                    -includedassemblies System.IO,System.Runtime.Intrinsics


    string list:    -excludedtypes              Optional. Comma separated list (no spaces) of specific types to ignore. Default is empty.

                                                Usage example:
                                                    -excludedtypes ArgumentException,Stream



    string list:    -includedtypes         Mandatory. Comma separated list (no spaces) of specific types to include. Default is empty and will include all types in the selected assemblies.

                                                Usage example:
                                                    -includedtypes FileStream,DirectoryInfo



    boo:            -printundoc             Optional. Will print a detailed summary of all the docs APIs that are undocumented. Default is false.

                                                Usage example:
                                                    -printundoc true



    bool:           -save                   Optional. Whether you want to save the changes in the dotnet-api-docs xml files. Default is false.

                                                Usage example:
                                                    -save true



    bool:           -skipexceptions         Optional. Whether you want exceptions to be ported or not. Setting this to false can result in a lot of noise because there is no way to
                                            detect if an exception has been ported already, but it went through language review and the original text was not preserved. Default is true (skips them).

                                                Usage example:
                                                    -skipexceptions false



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
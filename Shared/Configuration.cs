using System;
using System.Collections.Generic;
using System.IO;

namespace DocsPortingTool
{
    public static class Configuration
    {
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

        public static readonly string[] AllowedAssemblyPrefixes = new string[] { "System", "Microsoft", "Windows" };
        public static readonly string[] ForbiddenDirectories = new[] { "binplacePackages", "docs", "mscorlib", "native", "netfx", "netstandard", "pkg", "Product", "ref", "runtime", "shimsTargetRuntime", "testhost", "tests", "winrt" };

        public static readonly List<DirectoryInfo> DirsTripleSlashXmls = new List<DirectoryInfo>();
        public static readonly HashSet<string> IncludedAssemblies = new HashSet<string>();
        public static readonly HashSet<string> ExcludedAssemblies = new HashSet<string>();
        public static readonly HashSet<string> IncludedTypes = new HashSet<string>();
        public static readonly HashSet<string> ExcludedTypes = new HashSet<string>();

        public static bool Save { get; set; } = false;
        public static bool SkipExceptions { get; set; } = true;
        public static DirectoryInfo DirDocsXml { get; set; }
        public static bool PrintUndoc { get; set; } = false;

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

        public static void GetFromCommandLineArguments(string[] args)
        {
            Mode mode = Mode.Initial;

            Log.Info("Verifying CLI arguments...");

            if (args == null || args.Length == 0)
            {
                Log.LogErrorPrintHelpAndExit(Log.PrintHelp, "No arguments passed to the executable.");
            }

            foreach (string arg in args)
            {
                switch (mode)
                {
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

                    case Mode.ExcludedAssemblies:
                        {
                            string[] splittedArg = arg.Split(Separator, StringSplitOptions.RemoveEmptyEntries);

                            if (splittedArg.Length > 0)
                            {
                                Log.Working("Excluded assemblies:");
                                foreach (string assembly in splittedArg)
                                {
                                    Log.Working($" - {assembly}");
                                    ExcludedAssemblies.Add(assembly);
                                }
                            }
                            else
                            {
                                Log.LogErrorPrintHelpAndExit(Log.PrintHelp, "You must specify at least one assembly.");
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
                                    ExcludedTypes.Add(typeName);
                                }
                            }
                            else
                            {
                                Log.LogErrorPrintHelpAndExit(Log.PrintHelp, "You must specify at least one type name.");
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
                                    IncludedAssemblies.Add(assembly);
                                }
                            }
                            else
                            {
                                Log.LogErrorPrintHelpAndExit(Log.PrintHelp, "You must specify at least one assembly.");
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
                                    IncludedTypes.Add(typeName);
                                }
                            }
                            else
                            {
                                Log.LogErrorPrintHelpAndExit(Log.PrintHelp, "You must specify at least one type name.");
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
                                    Log.PrintHelp();
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
                                    Log.LogErrorPrintHelpAndExit(Log.PrintHelp, string.Format("Unrecognized argument '{0}'.", arg));
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
                                Log.LogErrorAndExit($"Invalid boolean value for the save argument: {arg}");
                            }

                            Save = save;

                            Log.Working("Save:");
                            Log.Info($"  -  {Save}");

                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.SkipExceptions:
                        {
                            if (!bool.TryParse(arg, out bool skipExceptions))
                            {
                                Log.LogErrorAndExit($"Invalid boolean value for the skipExceptions argument: {arg}");
                            }

                            SkipExceptions = skipExceptions;

                            Log.Working("Skip exceptions:");
                            Log.Info($"  -  {SkipExceptions}");

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
                            Log.LogErrorPrintHelpAndExit(Log.PrintHelp, "Unexpected mode.");
                            break;
                        }
                }
            }

            if (mode != Mode.Initial)
            {
                Log.LogErrorPrintHelpAndExit(Log.PrintHelp, "You missed an argument value.");
            }

            if (DirDocsXml == null)
            {
                Log.LogErrorPrintHelpAndExit(Log.PrintHelp, "You must specify a path to the dotnet-api-docs xml folder with -docs.");
            }

            if (DirsTripleSlashXmls.Count == 0)
            {
                Log.LogErrorPrintHelpAndExit(Log.PrintHelp, "You must specify at least one triple slash xml folder path with -tripleslash.");
            }

            if (IncludedAssemblies.Count == 0)
            {
                Log.LogErrorPrintHelpAndExit(Log.PrintHelp, "You must specify at least one assembly with -include.");
            }
        }
    }
}

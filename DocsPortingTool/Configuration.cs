using System;
using System.Collections.Generic;
using System.IO;

namespace DocsPortingTool
{
    public class Configuration
    {
        private static readonly char Separator = ',';

        private enum Mode
        {
            DisablePrompts,
            Docs,
            ExcludedAssemblies,
            ExcludedTypes,
            IncludedAssemblies,
            IncludedTypes,
            Initial,
            PrintUndoc,
            Save,
            SkipExceptions,
            SkipInterfaceImplementations,
            SkipRemarks,
            TripleSlash
        }

        public static readonly string ToBeAdded = "To be added.";

        public static readonly string[] ForbiddenDirectories = new[] { "binplacePackages", "docs", "mscorlib", "native", "netfx", "netstandard", "pkg", "Product", "ref", "runtime", "shimsTargetRuntime", "testhost", "tests", "winrt" };

        public List<DirectoryInfo> DirsTripleSlashXmls { get; } = new List<DirectoryInfo>();
        public List<DirectoryInfo> DirsDocsXml { get; } = new List<DirectoryInfo>();

        public HashSet<string> IncludedAssemblies { get; } = new HashSet<string>();
        public HashSet<string> ExcludedAssemblies { get; } = new HashSet<string>();
        public HashSet<string> IncludedTypes { get; } = new HashSet<string>();
        public HashSet<string> ExcludedTypes { get; } = new HashSet<string>();

        public bool Save { get; set; } = false;
        public bool SkipExceptions { get; set; } = true;
        public bool SkipRemarks { get; set; } = false;
        public bool SkipInterfaceImplementations { get; set; } = false;
        public bool DisablePrompts { get; set; } = false;
        public bool PrintUndoc { get; set; } = false;

        public static Configuration GetFromCommandLineArguments(string[] args)
        {
            Mode mode = Mode.Initial;

            Log.Info("Verifying CLI arguments...");

            if (args == null || args.Length == 0)
            {
                Log.LogErrorPrintHelpAndExit("No arguments passed to the executable.");
            }

            Configuration config = new Configuration();

            foreach (string arg in args!)
            {
                switch (mode)
                {
                    case Mode.DisablePrompts:
                        {
                            if (!bool.TryParse(arg, out bool disablePrompts))
                            {
                                Log.LogErrorAndExit($"Invalid boolean value for the disablePrompts argument: {arg}");
                            }

                            config.DisablePrompts = disablePrompts;

                            Log.Cyan("Disable prompts:");
                            Log.Info($"  -  {config.DisablePrompts}");

                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.Docs:
                        {
                            string[] splittedDirPaths = arg.Split(',', StringSplitOptions.RemoveEmptyEntries);

                            Log.Cyan($"Specified Docs xml locations:");
                            foreach (string dirPath in splittedDirPaths)
                            {
                                DirectoryInfo dirInfo = new DirectoryInfo(dirPath);
                                if (!dirInfo.Exists)
                                {
                                    Log.LogErrorAndExit($"This Docs xml directory does not exist: {dirPath}");
                                }

                                config.DirsDocsXml.Add(dirInfo);
                                Log.Info($"  -  {dirPath}");
                            }

                            mode = Mode.Initial;
                            break;

                        }

                    case Mode.ExcludedAssemblies:
                        {
                            string[] splittedArg = arg.Split(Separator, StringSplitOptions.RemoveEmptyEntries);

                            if (splittedArg.Length > 0)
                            {
                                Log.Cyan("Excluded assemblies:");
                                foreach (string assembly in splittedArg)
                                {
                                    Log.Cyan($" - {assembly}");
                                    config.ExcludedAssemblies.Add(assembly);
                                }
                            }
                            else
                            {
                                Log.LogErrorPrintHelpAndExit("You must specify at least one assembly.");
                            }

                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.ExcludedTypes:
                        {
                            string[] splittedArg = arg.Split(Separator, StringSplitOptions.RemoveEmptyEntries);

                            if (splittedArg.Length > 0)
                            {
                                Log.Cyan($"Excluded types:");
                                foreach (string typeName in splittedArg)
                                {
                                    Log.Cyan($" - {typeName}");
                                    config.ExcludedTypes.Add(typeName);
                                }
                            }
                            else
                            {
                                Log.LogErrorPrintHelpAndExit("You must specify at least one type name.");
                            }

                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.IncludedAssemblies:
                        {
                            string[] splittedArg = arg.Split(Separator, StringSplitOptions.RemoveEmptyEntries);

                            if (splittedArg.Length > 0)
                            {
                                Log.Cyan($"Included assemblies:");
                                foreach (string assembly in splittedArg)
                                {
                                    Log.Cyan($" - {assembly}");
                                    config.IncludedAssemblies.Add(assembly);
                                }
                            }
                            else
                            {
                                Log.LogErrorPrintHelpAndExit("You must specify at least one assembly.");
                            }

                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.IncludedTypes:
                        {
                            string[] splittedArg = arg.Split(Separator, StringSplitOptions.RemoveEmptyEntries);

                            if (splittedArg.Length > 0)
                            {
                                Log.Cyan($"Included types:");
                                foreach (string typeName in splittedArg)
                                {
                                    Log.Cyan($" - {typeName}");
                                    config.IncludedTypes.Add(typeName);
                                }
                            }
                            else
                            {
                                Log.LogErrorPrintHelpAndExit("You must specify at least one type name.");
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

                                case "-disableprompts":
                                    mode = Mode.DisablePrompts;
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

                                case "-skipinterfaceimplementations":
                                    mode = Mode.SkipInterfaceImplementations;
                                    break;

                                case "-skipremarks":
                                    mode = Mode.SkipRemarks;
                                    break;

                                case "-tripleslash":
                                    mode = Mode.TripleSlash;
                                    break;
                                default:
                                    Log.LogErrorPrintHelpAndExit($"Unrecognized argument: {arg}");
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

                            config.PrintUndoc = printUndoc;

                            Log.Cyan("Print undocumented:");
                            Log.Info($"  -  {config.PrintUndoc}");

                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.Save:
                        {
                            if (!bool.TryParse(arg, out bool save))
                            {
                                Log.LogErrorAndExit($"Invalid boolean value for the save argument: {arg}");
                            }

                            config.Save = save;

                            Log.Cyan("Save:");
                            Log.Info($"  -  {config.Save}");

                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.SkipExceptions:
                        {
                            if (!bool.TryParse(arg, out bool skipExceptions))
                            {
                                Log.LogErrorAndExit($"Invalid boolean value for the skipExceptions argument: {arg}");
                            }

                            config.SkipExceptions = skipExceptions;

                            Log.Cyan("Skip exceptions:");
                            Log.Info($"  -  {config.SkipExceptions}");

                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.SkipInterfaceImplementations:
                        {
                            if (!bool.TryParse(arg, out bool skipInterfaceImplementations))
                            {
                                Log.LogErrorAndExit($"Invalid boolean value for the skipInterfaceImplementations argument: {arg}");
                            }

                            config.SkipInterfaceImplementations = skipInterfaceImplementations;

                            Log.Cyan("Skip interface implementations:");
                            Log.Info($"  -  {config.SkipInterfaceImplementations}");

                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.SkipRemarks:
                        {
                            if (!bool.TryParse(arg, out bool skipRemarks))
                            {
                                Log.LogErrorAndExit($"Invalid boolean value for the skipRemarks argument: {arg}");
                            }

                            config.SkipRemarks = skipRemarks;

                            Log.Cyan("Skip remarks:");
                            Log.Info($"  -  {config.SkipRemarks}");

                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.TripleSlash:
                        {
                            string[] splittedDirPaths = arg.Split(',', StringSplitOptions.RemoveEmptyEntries);

                            Log.Cyan($"Specified triple slash locations:");
                            foreach (string dirPath in splittedDirPaths)
                            {
                                DirectoryInfo dirInfo = new DirectoryInfo(dirPath);
                                if (!dirInfo.Exists)
                                {
                                    Log.LogErrorAndExit($"This triple slash xml directory does not exist: {dirPath}");
                                }

                                config.DirsTripleSlashXmls.Add(dirInfo);
                                Log.Info($"  -  {dirPath}");
                            }

                            mode = Mode.Initial;
                            break;
                        }

                    default:
                        {
                            Log.LogErrorPrintHelpAndExit("Unexpected mode.");
                            break;
                        }
                }
            }

            if (mode != Mode.Initial)
            {
                Log.LogErrorPrintHelpAndExit("You missed an argument value.");
            }

            if (config.DirsDocsXml == null)
            {
                Log.LogErrorPrintHelpAndExit("You must specify a path to the dotnet-api-docs xml folder with -docs.");
            }

            if (config.DirsTripleSlashXmls.Count == 0)
            {
                Log.LogErrorPrintHelpAndExit("You must specify at least one triple slash xml folder path with -tripleslash.");
            }

            if (config.IncludedAssemblies.Count == 0)
            {
                Log.LogErrorPrintHelpAndExit("You must specify at least one assembly with -include.");
            }

            return config;
        }

        // Hardcoded namespaces that need to be renamed to what MS Docs uses.
        public static string ReplaceNamespace(string str)
        {
            return str.Replace("Microsoft.Data", "System.Data");
        }

    }
}

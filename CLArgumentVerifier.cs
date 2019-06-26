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
                Log.LogErrorPrintHelpAndExit("No arguments passed to the executable.");
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
                                Log.LogErrorPrintHelpAndExit("You must specify at least one assembly.");
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
                                Log.LogErrorPrintHelpAndExit("You must specify at least one assembly.");
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
                                    Log.PrintHelp();
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
                                    Log.LogErrorPrintHelpAndExit(string.Format("Unrecognized argument '{0}'.", arg));
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
                                Log.LogErrorPrintHelpAndExit("You must specify at least one path containing triple slash xml files.");
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

            if (PathDocsXml == null)
            {
                Log.LogErrorPrintHelpAndExit("You must specify a path to the dotnet-api-docs xml folder with -docs.");
            }

            if (PathsTripleSlashXmls.Count == 0)
            {
                Log.LogErrorPrintHelpAndExit("You must specify at least one triple slash xml folder path with -tripleslash.");
            }

            if (IncludedAssemblies.Count == 0)
            {
                Log.LogErrorPrintHelpAndExit("You must specify at least one assembly with -include.");
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

        #endregion
    }
}
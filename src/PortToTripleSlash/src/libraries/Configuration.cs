// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;

namespace ApiDocsSync.Libraries
{
    public class Configuration
    {
        private static readonly char s_separator = ',';

        private enum Mode
        {
            BinLogPath,
            CsProj,
            Docs,
            ExcludedAssemblies,
            ExcludedNamespaces,
            ExcludedTypes,
            IncludedAssemblies,
            IncludedNamespaces,
            IncludedTypes,
            Initial,
            IsMono,
            SkipInterfaceImplementations,
            SkipInterfaceRemarks
        }

        // The default boilerplate string for what dotnet-api-docs
        // considers an empty (undocumented) API element.
        public static readonly string ToBeAdded = "To be added.";

        public static readonly string[] ForbiddenBinSubdirectories = new[] {
            "binplacePackages",
            "docs",
            "externals",
            "mscorlib",
            "native",
            "netfx",
            "netstandard",
            "pkg",
            "Product",
            "ref",
            "runtime",
            "shimsTargetRuntime",
            "testhost",
            "tests",
            "winrt"
        };

        public MSBuildLoader? Loader { get; set; }

        public string? BinLogPath { get; set; }
        public string CsProj { get; set; } = string.Empty;
        public List<DirectoryInfo> DirsDocsXml { get; } = new List<DirectoryInfo>();
        public HashSet<string> ExcludedAssemblies { get; } = new HashSet<string>();
        public HashSet<string> ExcludedNamespaces { get; } = new HashSet<string>();
        public HashSet<string> ExcludedTypes { get; } = new HashSet<string>();
        public HashSet<string> IncludedAssemblies { get; } = new HashSet<string>();
        public HashSet<string> IncludedNamespaces { get; } = new HashSet<string>();
        public HashSet<string> IncludedTypes { get; } = new HashSet<string>();
        public bool IsMono { get; set; }
        public bool SkipInterfaceImplementations { get; set; } = false;
        public bool SkipInterfaceRemarks { get; set; } = true;

        public static Configuration GetCLIArguments(string[] args)
        {
            Mode mode = Mode.Initial;

            Log.Info("Verifying CLI arguments...");

            if (args == null || args.Length == 0)
            {
                Log.ErrorAndExit("No arguments passed to the executable.");
            }

            Configuration config = new();

            foreach (string arg in args!)
            {
                switch (mode)
                {
                    case Mode.BinLogPath:
                        {
                            if (string.IsNullOrWhiteSpace(arg))
                            {
                                throw new Exception("You must specify a *.binlog path.");
                            }
                            else if (Path.GetExtension(arg).ToUpperInvariant() != ".BINLOG")
                            {
                                throw new Exception($"The file does not have a *.binlog extension: {arg}");
                            }

                            config.BinLogPath = Path.GetFullPath(arg);

                            File.Delete(config.BinLogPath);

                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.CsProj:
                        {
                            if (string.IsNullOrWhiteSpace(arg))
                            {
                                throw new Exception("You must specify a *.csproj path.");
                            }
                            else if (!File.Exists(arg))
                            {
                                throw new Exception($"The *.csproj file does not exist: {arg}");
                            }
                            else if (Path.GetExtension(arg).ToUpperInvariant() != ".CSPROJ")
                            {
                                throw new Exception($"The file does not have a *.csproj extension: {arg}");
                            }
                            config.CsProj = Path.GetFullPath(arg);
                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.Docs:
                        {
                            string[] splittedDirPaths = arg.Split(',', StringSplitOptions.RemoveEmptyEntries);

                            Log.Cyan($"Specified Docs xml locations:");
                            foreach (string dirPath in splittedDirPaths)
                            {
                                DirectoryInfo dirInfo = new(dirPath);
                                if (!dirInfo.Exists)
                                {
                                    throw new Exception($"This Docs xml directory does not exist: {dirPath}");
                                }

                                config.DirsDocsXml.Add(dirInfo);
                                Log.Info($"  -  {dirPath}");
                            }

                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.ExcludedAssemblies:
                        {
                            string[] splittedArg = arg.Split(s_separator, StringSplitOptions.RemoveEmptyEntries);

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
                                Log.ErrorAndExit("You must specify at least one assembly.");
                            }

                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.ExcludedNamespaces:
                        {
                            string[] splittedArg = arg.Split(s_separator, StringSplitOptions.RemoveEmptyEntries);

                            if (splittedArg.Length > 0)
                            {
                                Log.Cyan("Excluded namespaces:");
                                foreach (string ns in splittedArg)
                                {
                                    Log.Cyan($" - {ns}");
                                    config.ExcludedNamespaces.Add(ns);
                                }
                            }
                            else
                            {
                                Log.ErrorAndExit("You must specify at least one namespace.");
                            }

                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.ExcludedTypes:
                        {
                            string[] splittedArg = arg.Split(s_separator, StringSplitOptions.RemoveEmptyEntries);

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
                                Log.ErrorAndExit("You must specify at least one type name.");
                            }

                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.IncludedAssemblies:
                        {
                            string[] splittedArg = arg.Split(s_separator, StringSplitOptions.RemoveEmptyEntries);

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
                                Log.ErrorAndExit("You must specify at least one assembly.");
                            }

                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.IncludedNamespaces:
                        {
                            string[] splittedArg = arg.Split(s_separator, StringSplitOptions.RemoveEmptyEntries);

                            if (splittedArg.Length > 0)
                            {
                                Log.Cyan($"Included namespaces:");
                                foreach (string ns in splittedArg)
                                {
                                    Log.Cyan($" - {ns}");
                                    config.IncludedNamespaces.Add(ns);
                                }
                            }
                            else
                            {
                                Log.ErrorAndExit("You must specify at least one namespace.");
                            }

                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.IncludedTypes:
                        {
                            string[] splittedArg = arg.Split(s_separator, StringSplitOptions.RemoveEmptyEntries);

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
                                Log.ErrorAndExit("You must specify at least one type name.");
                            }

                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.Initial:
                        {
                            switch (arg.ToUpperInvariant())
                            {
                                case "-BINLOGPATH":
                                    mode = Mode.BinLogPath;
                                    break;

                                case "-CSPROJ":
                                    mode = Mode.CsProj;
                                    break;

                                case "-DOCS":
                                    mode = Mode.Docs;
                                    break;

                                case "-EXCLUDEDASSEMBLIES":
                                    mode = Mode.ExcludedAssemblies;
                                    break;

                                case "-EXCLUDEDNAMESPACES":
                                    mode = Mode.ExcludedNamespaces;
                                    break;

                                case "-EXCLUDEDTYPES":
                                    mode = Mode.ExcludedTypes;
                                    break;

                                case "-H":
                                case "-HELP":
                                    Log.PrintHelp();
                                    Environment.Exit(0);
                                    break;

                                case "-INCLUDEDASSEMBLIES":
                                    mode = Mode.IncludedAssemblies;
                                    break;

                                case "-INCLUDEDNAMESPACES":
                                    mode = Mode.IncludedNamespaces;
                                    break;

                                case "-INCLUDEDTYPES":
                                    mode = Mode.IncludedTypes;
                                    break;

                                case "-ISMONO":
                                    mode = Mode.IsMono;
                                    break;

                                case "-SKIPINTERFACEIMPLEMENTATIONS":
                                    mode = Mode.SkipInterfaceImplementations;
                                    break;

                                case "-SKIPINTERFACEREMARKS":
                                    mode = Mode.SkipInterfaceRemarks;
                                    break;

                                default:
                                    Log.ErrorAndExit($"Unrecognized argument: {arg}");
                                    break;
                            }
                            break;
                        }

                    case Mode.IsMono:
                        {
                            config.IsMono = ParseOrExit(arg, nameof(Mode.IsMono));
                            break;
                        }

                    case Mode.SkipInterfaceImplementations:
                        {
                            config.SkipInterfaceImplementations = ParseOrExit(arg, nameof(Mode.SkipInterfaceImplementations));
                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.SkipInterfaceRemarks:
                        {
                            config.SkipInterfaceRemarks = ParseOrExit(arg, nameof(Mode.SkipInterfaceRemarks));
                            mode = Mode.Initial;
                            break;
                        }

                    default:
                        {
                            Log.ErrorAndExit("Unexpected mode.");
                            break;
                        }
                }
            }

            if (mode != Mode.Initial)
            {
                Log.ErrorAndExit("You missed an argument value.");
            }

            if (config.DirsDocsXml == null)
            {
                Log.ErrorAndExit($"You must specify a path to the dotnet-api-docs xml folder using '-{nameof(Mode.Docs)}'.");
            }

            if (string.IsNullOrEmpty(config.CsProj))
            {
                Log.ErrorAndExit($"You must specify a *.csproj file using '-{nameof(Mode.CsProj)}'.");
            }

            if (!File.Exists(config.CsProj))
            {
                Log.ErrorAndExit($"The file specified with '-{nameof(Mode.CsProj)}' does not exist: {config.CsProj}");
            }

            if (config.IncludedAssemblies.Count == 0)
            {
                Log.ErrorAndExit($"You must specify at least one assembly with {nameof(IncludedAssemblies)}.");
            }

            return config;
        }

        // Tries to parse the user argument string as boolean, and if it fails, exits the program.
        private static bool ParseOrExit(string arg, string nameofParam)
        {
            if (!bool.TryParse(arg, out bool value))
            {
                throw new Exception($"Invalid boolean value for '{nameofParam}' argument: {arg}");
            }

            Log.Cyan($"{nameofParam}:");
            Log.Info($" - {value}");

            return value;
        }
    }
}

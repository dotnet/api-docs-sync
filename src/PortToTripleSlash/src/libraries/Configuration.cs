// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;

namespace ApiDocsSync.PortToTripleSlash
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
                                    PrintHelp();
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

        public static void PrintHelp()
        {
            Log.Cyan(@"
This tool finds and ports triple slash comments found in .NET repos but do not yet exist in the dotnet-api-docs repo.
The instructions below assume %SourceRepos% is the root folder of all your git cloned projects.
Options:
                               MANDATORY
  ------------------------------------------------------------
  |    PARAMETER     |           TYPE          | DESCRIPTION |
  ------------------------------------------------------------
    -Docs                    comma-separated    A comma separated list (no spaces) of absolute directory paths where the Docs xml files are located.
                                                    The xml files will be searched for recursively.
                                                    If any of the segments in the path may contain spaces, make sure to enclose the path in double quotes.
                             folder paths           Known locations:
                                                        > Runtime:      %SourceRepos%\dotnet-api-docs\xml
                                                        > WPF:          %SourceRepos%\dotnet-api-docs\xml
                                                        > WinForms:     %SourceRepos%\dotnet-api-docs\xml
                                                        > ASP.NET MVC:  %SourceRepos%\AspNetApiDocs\aspnet-mvc\xml
                                                        > ASP.NET Core: %SourceRepos%\AspNetApiDocs\aspnet-core\xml
                                                    Usage example:
                                                        -Docs ""%SourceRepos%\dotnet-api-docs\xml\System.IO.FileSystem\"",%SourceRepos%\AspNetApiDocs\aspnet-mvc\xml
    -IncludedAssemblies     string list         Comma separated list (no spaces) of assemblies to include.
                                                This argument prevents loading everything in the specified folder.
                                                    Usage example:
                                                        -IncludedAssemblies System.IO,System.Runtime
                                                    IMPORTANT:
                                                    Namespaces usually match the assembly name. There are some exceptions, like with types that live in
                                                    the System.Runtime assembly. For those cases, make sure to also specify the -IncludedNamespaces argument.
                                                    IMPORTANT:
                                                    Include both facades and implementation. For example, for System.IO.FileStream, include both
                                                    System.Private.CoreLib (for the implementation) and System.Runtime (the facade).
    -CsProj                 file path           Mandatory.
                                                    An absolute path to a *.csproj file from your repo. Make sure its the src file, not the ref or test file.
                                                    Known locations:
                                                        > Runtime:   %SourceRepos%\runtime\src\libraries\<AssemblyOrNamespace>\src\<AssemblyOrNamespace>.csproj
                                                        > CoreCLR:   %SourceRepos%\runtime\src\coreclr\src\System.Private.CoreLib\System.Private.CoreLib.csproj
                                                        > WPF:       %SourceRepos%\wpf\src\Microsoft.DotNet.Wpf\src\<AssemblyOrNamespace>\<AssemblyOrNamespace>.csproj
                                                        > WinForms:  %SourceRepos%\winforms\src\<AssemblyOrNamespace>\src\<AssemblyOrNamespace>.csproj
                                                        > WCF:       %SourceRepos%\wcf\src\<AssemblyOrNamespace>\
                                                    Usage example:
                                                        -SourceCode ""%SourceRepos%\runtime\src\libraries\System.IO.FileSystem\"",%SourceRepos%\runtime\src\coreclr\src\System.Private.CoreLib\
                               OPTIONAL
  ------------------------------------------------------------
  |    PARAMETER     |           TYPE          | DESCRIPTION |
  ------------------------------------------------------------
    -h | -Help              no arguments        Displays this help message. If used, all other arguments are ignored and the program exits.
    -BinLogPath             string              Default is null (binlog file generation is disabled).
                                                When set to a valid path, will output a diagnostics binlog to that location.
    -ExcludedAssemblies     string list         Default is empty (does not ignore any assemblies/namespaces).
                                                Comma separated list (no spaces) of specific .NET assemblies/namespaces to ignore.
                                                    Usage example:
                                                        -ExcludedAssemblies System.IO.Compression,System.IO.Pipes
    -ExcludedNamespaces     string list         Default is empty (does not exclude any namespaces from the specified assemblies).
                                                Comma separated list (no spaces) of specific namespaces to exclude from the specified assemblies.
                                                    Usage example:
                                                        -ExcludedNamespaces System.Runtime.Intrinsics,System.Reflection.Metadata
    -ExcludedTypes          string list         Default is empty (does not ignore any types).
                                                Comma separated list (no spaces) of names of types to ignore.
                                                    Usage example:
                                                        -ExcludedTypes ArgumentException,Stream
    -IncludedNamespaces     string list         Default is empty (includes all namespaces from the specified assemblies).
                                                Comma separated list (no spaces) of specific namespaces to include from the specified assemblies.
                                                    Usage example:
                                                        -IncludedNamespaces System,System.Data
    -IncludedTypes          string list         Default is empty (includes all types in the desired assemblies/namespaces).
                                                Comma separated list (no spaces) of specific types to include.
                                                    Usage example:
                                                        -IncludedTypes FileStream,DirectoryInfo
    -IsMono                 bool                Default is false.
                                                When set to true, the main project passed with -CsProj is assumed to be a mono project.
    -SkipInterfaceImplementations       bool    Default is false (includes interface implementations).
                                                Whether you want the original interface documentation to be considered to fill the
                                                undocumented API's documentation when the API itself does not provide its own documentation.
                                                Setting this to false will include Explicit Interface Implementations as well.
                                                    Usage example:
                                                        -SkipInterfaceImplementations true
     -SkipInterfaceRemarks              bool    Default is true (excludes appending interface remarks).
                                                Whether you want interface implementation remarks to be used when the API itself has no remarks.
                                                Very noisy and generally the content in those remarks do not apply to the API that implements
                                                the interface API.
                                                    Usage example:
                                                        -SkipInterfaceRemarks false
            ");
            Log.Warning(@"
    TL;DR:

      PortToTripleSlash
        -CsProj <pathToCsproj>
        -Docs <pathToDocsXmlFolder>
        -IncludedAssemblies <assembly1>[,<assembly2>,...,<assemblyN>]
        -IncludedNamespaces <namespace1>[,<namespace2>,...,<namespaceN>]

        Example:
            PortToTripleSlash \
                -CsProj D:\runtime\src\libraries\System.IO.Compression.Brotli\src\System.IO.Compression.Brotli.csproj \
                -Docs D:\dotnet-api-docs\xml \
                -IncludedAssemblies System.IO.Compression.Brotli \
                -IncludedNamespaces System.IO.Compression \
");
            Log.Magenta(@"
    Note:
        If the assembly and the namespace is exactly the same, you can skip the -IncludedNamespaces argument.
            ");
        }
    }
}

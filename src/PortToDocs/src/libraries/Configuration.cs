// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;

namespace ApiDocsSync.PortToDocs
{
    public class Configuration
    {
        public const string NewLine = "\n";
        private static readonly char Separator = ',';

        private enum Mode
        {
            BinLog,
            DisablePrompts,
            Docs,
            ExceptionCollisionThreshold,
            ExcludedAssemblies,
            ExcludedNamespaces,
            ExcludedTypes,
            IncludedAssemblies,
            IncludedNamespaces,
            IncludedTypes,
            Initial,
            IntelliSense,
            MarkdownRemarks,
            PortExceptionsExisting,
            PortExceptionsNew,
            PortMemberParams,
            PortMemberProperties,
            PortMemberReturns,
            PortMemberRemarks,
            PortMemberSeeAlsos,
            PortMemberSummaries,
            PortMemberTypeParams,
            PortTypeParams, // Params of a Type
            PortTypeRemarks,
            PortTypeSeeAlsos,
            PortTypeSummaries,
            PortTypeTypeParams, // TypeParams of a Type
            PreserveInheritDocTag,
            PrintSummaryDetails,
            PrintUndoc,
            Save,
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

        public readonly string BinLogPath = "output.binlog";
        public bool BinLogger { get; private set; } = false;
        public List<DirectoryInfo> DirsIntelliSense { get; } = new List<DirectoryInfo>();
        public List<DirectoryInfo> DirsDocsXml { get; } = new List<DirectoryInfo>();
        public bool DisablePrompts { get; set; } = true;
        public int ExceptionCollisionThreshold { get; set; } = 70;
        public HashSet<string> ExcludedAssemblies { get; } = new HashSet<string>();
        public HashSet<string> ExcludedNamespaces { get; } = new HashSet<string>();
        public HashSet<string> ExcludedTypes { get; } = new HashSet<string>();
        public HashSet<string> IncludedAssemblies { get; } = new HashSet<string>();
        public HashSet<string> IncludedNamespaces { get; } = new HashSet<string>();
        public HashSet<string> IncludedTypes { get; } = new HashSet<string>();
        public bool MarkdownRemarks { get; set; } = false;
        public bool PortExceptionsExisting { get; set; } = false;
        public bool PortExceptionsNew { get; set; } = true;
        public bool PortMemberParams { get; set; } = true;
        public bool PortMemberProperties { get; set; } = true;
        public bool PortMemberReturns { get; set; } = true;
        public bool PortMemberRemarks { get; set; } = true;
        public bool PortMemberSeeAlsos { get; set; } = true;
        public bool PortMemberSummaries { get; set; } = true;
        public bool PortMemberTypeParams { get; set; } = true;
        /// <summary>
        /// Params of a Type.
        /// </summary>
        public bool PortTypeParams { get; set; } = true;
        public bool PortTypeRemarks { get; set; } = true;
        public bool PortTypeSeeAlsos { get; set; } = true;
        public bool PortTypeSummaries { get; set; } = true;
        /// <summary>
        /// TypeParams of a Type.
        /// </summary>
        public bool PortTypeTypeParams { get; set; } = true;
        public bool PreserveInheritDocTag { get; set; } = true;
        public bool PrintSummaryDetails { get; set; } = false;
        public bool PrintUndoc { get; set; } = false;
        public bool Save { get; set; } = false;
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

            Configuration config = new Configuration();

            foreach (string arg in args!)
            {
                switch (mode)
                {
                    case Mode.BinLog:
                        {
                            config.BinLogger = ParseOrExit(arg, "Create a binlog");
                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.DisablePrompts:
                        {
                            config.DisablePrompts = ParseOrExit(arg, "Disable prompts");
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
                                    throw new Exception($"This Docs xml directory does not exist: {dirPath}");
                                }

                                config.DirsDocsXml.Add(dirInfo);
                                Log.Info($"  -  {dirPath}");
                            }

                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.ExceptionCollisionThreshold:
                        {
                            if (!int.TryParse(arg, out int value))
                            {
                                throw new Exception($"Invalid int value for 'Exception collision threshold' argument: {arg}");
                            }
                            else if (value < 1 || value > 100)
                            {
                                throw new Exception($"Value needs to be between 0 and 100: {value}");
                            }

                            config.ExceptionCollisionThreshold = value;

                            Log.Cyan($"Exception collision threshold:");
                            Log.Info($" - {value}");
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
                                Log.ErrorAndExit("You must specify at least one assembly.");
                            }

                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.ExcludedNamespaces:
                        {
                            string[] splittedArg = arg.Split(Separator, StringSplitOptions.RemoveEmptyEntries);

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
                                Log.ErrorAndExit("You must specify at least one type name.");
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
                                Log.ErrorAndExit("You must specify at least one assembly.");
                            }

                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.IncludedNamespaces:
                        {
                            string[] splittedArg = arg.Split(Separator, StringSplitOptions.RemoveEmptyEntries);

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
                                Log.ErrorAndExit("You must specify at least one type name.");
                            }

                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.Initial:
                        {
                            switch (arg.ToUpperInvariant())
                            {
                                case "-BINLOG":
                                    mode = Mode.BinLog;
                                    break;

                                case "-DOCS":
                                    mode = Mode.Docs;
                                    break;

                                case "-DISABLEPROMPTS":
                                    mode = Mode.DisablePrompts;
                                    break;

                                case "EXCEPTIONCOLLISIONTHRESHOLD":
                                    mode = Mode.ExceptionCollisionThreshold;
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

                                case "-INTELLISENSE":
                                    mode = Mode.IntelliSense;
                                    break;

                                case "-MARKDOWNREMARKS":
                                    mode = Mode.MarkdownRemarks;
                                    break;

                                case "-PORTEXCEPTIONSEXISTING":
                                    mode = Mode.PortExceptionsExisting;
                                    break;

                                case "-PORTEXCEPTIONSNEW":
                                    mode = Mode.PortExceptionsNew;
                                    break;

                                case "-PORTMEMBERPARAMS":
                                    mode = Mode.PortMemberParams;
                                    break;

                                case "-PORTMEMBERPROPERTIES":
                                    mode = Mode.PortMemberProperties;
                                    break;

                                case "-PORTMEMBERRETURNS":
                                    mode = Mode.PortMemberReturns;
                                    break;

                                case "-PORTMEMBERREMARKS":
                                    mode = Mode.PortMemberRemarks;
                                    break;

                                case "-PORTMEMBERSUMMARIES":
                                    mode = Mode.PortMemberSummaries;
                                    break;

                                case "-PORTMEMBERSEEALSOS":
                                    mode = Mode.PortMemberSeeAlsos;
                                    break;

                                case "-PORTMEMBERTYPEPARAMS":
                                    mode = Mode.PortMemberTypeParams;
                                    break;

                                case "-PORTTYPEPARAMS": // Params of a Type
                                    mode = Mode.PortTypeParams;
                                    break;

                                case "-PORTTYPEREMARKS":
                                    mode = Mode.PortTypeRemarks;
                                    break;

                                case "-PORTTYPESEEALSOS":
                                    mode = Mode.PortTypeSeeAlsos;
                                    break;

                                case "-PORTTYPESUMMARIES":
                                    mode = Mode.PortTypeSummaries;
                                    break;

                                case "-PORTTYPETYPEPARAMS": // TypeParams of a Type
                                    mode = Mode.PortTypeTypeParams;
                                    break;

                                case "-PRESERVEINHERITDOCTAG":
                                    mode = Mode.PreserveInheritDocTag;
                                    break;

                                case "-PRINTSUMMARYDETAILS":
                                    mode = Mode.PrintSummaryDetails;
                                    break;

                                case "-PRINTUNDOC":
                                    mode = Mode.PrintUndoc;
                                    break;

                                case "-SAVE":
                                    mode = Mode.Save;
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

                    case Mode.IntelliSense:
                        {
                            string[] splittedDirPaths = arg.Split(',', StringSplitOptions.RemoveEmptyEntries);

                            Log.Cyan($"Specified IntelliSense locations:");
                            foreach (string dirPath in splittedDirPaths)
                            {
                                DirectoryInfo dirInfo = new DirectoryInfo(dirPath);
                                if (!dirInfo.Exists)
                                {
                                    throw new Exception($"This IntelliSense directory does not exist: {dirPath}");
                                }

                                config.DirsIntelliSense.Add(dirInfo);
                                Log.Info($"  -  {dirPath}");
                            }

                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.MarkdownRemarks:
                        {
                            config.MarkdownRemarks = ParseOrExit(arg, "Port remarks in markdown");
                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.PortExceptionsExisting:
                        {
                            config.PortExceptionsExisting = ParseOrExit(arg, "Port existing exceptions");
                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.PortExceptionsNew:
                        {
                            config.PortExceptionsNew = ParseOrExit(arg, "Port new exceptions");
                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.PortMemberParams:
                        {
                            config.PortMemberParams = ParseOrExit(arg, "Port member Params");
                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.PortMemberProperties:
                        {
                            config.PortMemberProperties = ParseOrExit(arg, "Port member Properties");
                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.PortMemberRemarks:
                        {
                            config.PortMemberRemarks = ParseOrExit(arg, "Port member Remarks");
                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.PortMemberReturns:
                        {
                            config.PortMemberReturns = ParseOrExit(arg, "Port member Returns");
                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.PortMemberSeeAlsos:
                        {
                            config.PortMemberSeeAlsos = ParseOrExit(arg, "Port member SeeAlsos");
                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.PortMemberSummaries:
                        {
                            config.PortMemberSummaries = ParseOrExit(arg, "Port member Summaries");
                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.PortMemberTypeParams:
                        {
                            config.PortMemberTypeParams = ParseOrExit(arg, "Port member TypeParams");
                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.PortTypeParams: // Params of a Type
                        {
                            config.PortTypeParams = ParseOrExit(arg, "Port Type Params");
                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.PortTypeRemarks:
                        {
                            config.PortTypeRemarks = ParseOrExit(arg, "Port Type Remarks");
                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.PortTypeSeeAlsos:
                        {
                            config.PortTypeSeeAlsos = ParseOrExit(arg, "Port type SeeAlsos");
                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.PortTypeSummaries:
                        {
                            config.PortTypeSummaries = ParseOrExit(arg, "Port Type Summaries");
                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.PortTypeTypeParams: // TypeParams of a Type
                        {
                            config.PortTypeTypeParams = ParseOrExit(arg, "Port Type TypeParams");
                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.PreserveInheritDocTag:
                        {
                            config.PreserveInheritDocTag = ParseOrExit(arg, "Preserve inheritdoc tag");
                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.PrintSummaryDetails:
                        {
                            config.PrintSummaryDetails = ParseOrExit(arg, "Print summary details");
                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.PrintUndoc:
                        {
                            config.PrintUndoc = ParseOrExit(arg, "Print undoc");
                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.Save:
                        {
                            config.Save = ParseOrExit(arg, "Save");
                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.SkipInterfaceImplementations:
                        {
                            config.SkipInterfaceImplementations = ParseOrExit(arg, "Skip interface implementations");
                            mode = Mode.Initial;
                            break;
                        }

                    case Mode.SkipInterfaceRemarks:
                        {
                            config.SkipInterfaceRemarks = ParseOrExit(arg, "Skip appending interface remarks");
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

            if (config.IncludedAssemblies.Count == 0)
            {
                Log.ErrorAndExit($"You must specify at least one assembly with {nameof(IncludedAssemblies)}.");
            }

            if (!config.PreserveInheritDocTag)
            {
                // TODO
                throw new NotSupportedException("Setting preverve inheritdoc tag to false is not yet supported.");
            }

            return config;
        }

        internal void VerifyIntellisenseXmlFiles()
        {
            if (DirsIntelliSense.Count == 0)
            {
                Log.ErrorAndExit($"You must specify at least one IntelliSense & DLL folder using '-{nameof(Mode.IntelliSense)}'.");
            }
        }

        internal void VerifyDocsFiles()
        {
            if (DirsDocsXml.Count == 0)
            {
                Log.ErrorAndExit($"You must specify a path to the dotnet-api-docs xml folder using '-{nameof(Mode.Docs)}'.");
            }
        }

        // Tries to parse the user argument string as boolean, and if it fails, exits the program.
        private static bool ParseOrExit(string arg, string paramFriendlyName)
        {
            if (!bool.TryParse(arg, out bool value))
            {
                throw new Exception($"Invalid boolean value for '{paramFriendlyName}' argument: {arg}");
            }

            Log.Cyan($"{paramFriendlyName}:");
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

    -IntelliSense           comma-separated     Mandatory.
                            folder paths            A comma separated list (no spaces) of absolute directory paths where we the IntelliSense xml files
                                                    are located. Usually it's the 'artifacts/bin' folder in your source code repo.
                                                    The IntelliSense xml files will be searched for recursively. You must specify the root folder (usually 'bin'),
                                                    which contains all the subfolders whose names are assemblies or namespaces. Only those names specified
                                                    with '-IncludedAssemblies' and '-IncludedNamespaces' will be recursed.
                                                    If any of the segments in the path may contain spaces, make sure to enclose the path in double quotes.
                                                    Known locations:
                                                        > Runtime:   %SourceRepos%\runtime\artifacts\bin\
                                                        > CoreCLR:   %SourceRepos%\runtime\artifacts\bin\coreclr\Windows_NT.x64.Release\IL\
                                                        > WinForms:  %SourceRepos%\winforms\artifacts\bin\
                                                        > WPF:       %SourceRepos%\wpf\artifacts\bin\
                                                    Usage example:
                                                        -IntelliSense ""%SourceRepos%\corefx\artifacts\bin\"",%SourceRepos%\winforms\artifacts\bin\

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

                               OPTIONAL
  ------------------------------------------------------------
  |    PARAMETER     |           TYPE          | DESCRIPTION |
  ------------------------------------------------------------

    -h | -Help              no arguments        Displays this help message. If used, all other arguments are ignored and the program exits.

    -BinLog                 bool                Default is false (binlog file generation is disabled).
                                                When set to true, will output a diagnostics binlog file.

    -DisablePrompts         bool                Default is true (prompts are disabled).
                                                Avoids prompting the user for input to correct some particular errors.
                                                    Usage example:
                                                        -DisablePrompts false

    -ExceptionCollisionThreshold  int (0-100)   Default is 70 (If >=70% of words collide, the string is not ported).
                                                Decides how sensitive the detection of existing exception strings should be.
                                                The tool compares the Docs exception string with the IntelliSense xml exception string.
                                                If the number of words found in the Docs exception is below the specified threshold,
                                                then the IntelliSense Xml string is appended at the end of the Docs string.
                                                The user is expected to verify the value.
                                                The reason for this is that exceptions go through language review, and may contain more
                                                than one root cause (separated by '-or-'), and there is no easy way to know if the string
                                                has already been ported or not.
                                                    Usage example:
                                                        -ExceptionCollisionThreshold 60

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

    -MarkdownRemarks            bool            Default is false (does not port remarks in markdown).
                                                When set to true, the remarks are ported to Docs using markdown language
                                                inside a CDATA element.
                                                When set to false, the remarks are ported in the default ECMAXml format.

    -PortExceptionsExisting     bool            Default is false (does not find and append existing exceptions).
                                                Enable or disable finding, porting and appending summaries from existing exceptions.
                                                Setting this to true can result in a lot of noise because there is
                                                no easy way to detect if an exception summary has been ported already or not,
                                                especially after it went through language review.
                                                See `-ExceptionCollisionThreshold` to set the collision sensitivity.
                                                    Usage example:
                                                        -PortExceptionsExisting true

    -PortExceptionsNew          bool            Default is true (ports new exceptions).
                                                Enable or disable finding and porting new exceptions.
                                                    Usage example:
                                                        -PortExceptionsNew false

    -PortMemberParams           bool            Default is true (ports Member parameters).
                                                Enable or disable finding and porting Member parameters.
                                                    Usage example:
                                                        -PortMemberParams false

    -PortMemberProperties       bool            Default is true (ports Member properties).
                                                Enable or disable finding and porting Member properties.
                                                    Usage example:
                                                        -PortMemberProperties false

    -PortMemberReturns          bool            Default is true (ports Member return values).
                                                Enable or disable finding and porting Member return values.
                                                    Usage example:
                                                        -PortMemberReturns false

    -PortMemberRemarks          bool            Default is true (ports Member remarks).
                                                Enable or disable finding and porting Member remarks.
                                                    Usage example:
                                                        -PortMemberRemarks false

    -PortMemberSeeAlsos         bool            Default is true (ports Member seealsos).
                                                Enable or disable finding and porting Member seealsos.
                                                These are found directly under the Docs xml element.
                                                    Usage example:
                                                        -PortMemberSeeAlsos false

    -PortMemberSummaries        bool            Default is true (ports Member summaries).
                                                Enable or disable finding and porting Member summaries.
                                                    Usage example:
                                                        -PortMemberSummaries false

    -PortMemberTypeParams       bool            Default is true (ports Member TypeParams).
                                                Enable or disable finding and porting Member TypeParams.
                                                    Usage example:
                                                        -PortMemberTypeParams false

    -PortTypeParams             bool            Default is true (ports Type Params).
                                                Enable or disable finding and porting Type Params.
                                                    Usage example:
                                                        -PortTypeParams false

    -PortTypeRemarks            bool            Default is true (ports Type remarks).
                                                Enable or disable finding and porting Type remarks.
                                                    Usage example:
                                                        -PortTypeRemarks false

    -PortTypeSeeAlsos           bool            Default is true (ports Type seealsos).
                                                Enable or disable finding and porting Type seealsos.
                                                These are found directly under the Docs xml element.
                                                    Usage example:
                                                        -PortTypeSeeAlsos false

    -PortTypeSummaries          bool            Default is true (ports Type summaries).
                                                Enable or disable finding and porting Type summaries.
                                                    Usage example:
                                                        -PortTypeSummaries false

    -PortTypeTypeParams         bool            Default is true (ports Type TypeParams).
                                                Enable or disable finding and porting Type TypeParams.
                                                    Usage example:
                                                        -PortTypeTypeParams false

    -PreserveInheritDocTag      bool            Default is true (preserves and ports the inheritdoc tag).
                                                If set to true, and an intellisense xml API has an inheritdoc element,
                                                then the element itself is ported to the docs xml API. If the intellisense
                                                xml API is missing any elements, their documentation is considered inherited,
                                                and the docs xml elements will remain empty ('To be added.'). MS Docs will
                                                show the interface or base type documentation automatically.
                                                If set to false, the inheritdoc element is not ported, and instead the
                                                tool will locate the interface or base type and port all the documentation strings.
                                                Regardless of the value of this command line option, if individual elements are
                                                documented in the intellisense xml, even though there is an inheritdoc tag found,
                                                these strings will always get ported to the docs xml.

    -PrintSummaryDetails        bool            Default is false (does not print summary details).
                                                Prints the list of APIs that got modified by the tool.
                                                    Usage example:
                                                        -PrintSummaryDetails true

    -PrintUndoc                 bool            Default is false (prints a basic summary).
                                                Prints a detailed summary of all the docs APIs that are undocumented.
                                                    Usage example:
                                                        -PrintUndoc true

    -Save                       bool            Default is false (does not save changes).
                                                Indicates whether you want to save the Docs xml file changes.
                                                    Usage example:
                                                        -Save true

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

      PortToDocs
        -Docs <pathToDocsXmlFolder>
        -IntelliSense <pathToArtifactsFolder1>[,<pathToArtifactsFolder2>,...,<pathToArtifactsFolderN>]
        -IncludedAssemblies <assembly1>[,<assembly2>,...<assemblyN>]
        -IncludedNamespaces <namespace1>[,<namespace2>,...,<namespaceN>]
        -Save true

        Example:
            PortToDocs \
                -Docs D:\dotnet-api-docs\xml \
                -IntelliSense D:\runtime\artifacts\bin\System.IO.FileSystem\ \
                -IncludedAssemblies System.IO.FileSystem \
                -IncludedNamespaces System.IO \
                -Save true
");
            Log.Magenta(@"
    Note:
        If the assembly and the namespace is exactly the same, you can skip the -IncludedNamespaces argument.

            ");
        }
    }
}

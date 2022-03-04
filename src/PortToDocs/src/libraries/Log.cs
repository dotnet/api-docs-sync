using System;

namespace DocsPortingTool.Libraries
{
    public static class Log
    {
        public static void Print(bool endline, ConsoleColor foregroundColor, string format, params object[]? args)
        {
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = foregroundColor;

            string msg = args != null ? (args.Length > 0 ? string.Format(format, args) : format) : format;
            if (endline)
            {
                Console.WriteLine(msg);
            }
            else
            {
                Console.Write(msg);
            }
            Console.ForegroundColor = originalColor;
        }

        public static void Info(string format)
        {
            Info(format, null);
        }

        public static void Info(string format, params object[]? args)
        {
            Info(true, format, args);
        }

        public static void Info(bool endline, string format, params object[]? args)
        {
            Print(endline, ConsoleColor.White, format, args);
        }

        public static void Success(string format)
        {
            Success(format, null);
        }

        public static void Success(string format, params object[]? args)
        {
            Success(true, format, args);
        }

        public static void Success(bool endline, string format, params object[]? args)
        {
            Print(endline, ConsoleColor.Green, format, args);
        }

        public static void Warning(string format)
        {
            Warning(format, null);
        }

        public static void Warning(string format, params object[]? args)
        {
            Warning(true, format, args);
        }

        public static void Warning(bool endline, string format, params object[]? args)
        {
            Print(endline, ConsoleColor.Yellow, format, args);
        }

        public static void Error(string format)
        {
            Error(format, null);
        }

        public static void Error(string format, params object[]? args)
        {
            Error(true, format, args);
        }

        public static void Error(bool endline, string format, params object[]? args)
        {
            Print(endline, ConsoleColor.Red, format, args);
        }

        public static void Cyan(string format)
        {
            Cyan(format, null);
        }

        public static void Cyan(string format, params object[]? args)
        {
            Cyan(true, format, args);
        }

        public static void Cyan(bool endline, string format, params object[]? args)
        {
            Print(endline, ConsoleColor.Cyan, format, args);
        }

        public static void Magenta(bool endline, string format, params object[]? args)
        {
            Print(endline, ConsoleColor.Magenta, format, args);
        }

        public static void Magenta(string format)
        {
            Magenta(format, null);
        }

        public static void Magenta(string format, params object[]? args)
        {
            Magenta(true, format, args);
        }

        public static void DarkYellow(bool endline, string format, params object[]? args)
        {
            Print(endline, ConsoleColor.DarkYellow, format, args);
        }

        public static void DarkYellow(string format)
        {
            DarkYellow(format, null);
        }

        public static void DarkYellow(string format, params object[]? args)
        {
            DarkYellow(true, format, args);
        }

        public static void Assert(bool condition, string format)
        {
            Assert(true, condition, format, null);
        }

        public static void Assert(bool condition, string format, params object[]? args)
        {
            Assert(true, condition, format, args);
        }

        public static void Assert(bool endline, bool condition, string format, params object[]? args)
        {
            if (condition)
            {
                Success(endline, format, args);
            }
            else
            {
                string msg = args != null ? string.Format(format, args) : format;
                throw new Exception(msg);
            }
        }

        public static void Line()
        {
            Print(endline: true, Console.ForegroundColor, "", null);
        }

        public delegate void PrintHelpFunction();

        public static void ErrorAndExit(string format, params object[]? args)
        {
            Error(format, args);
            Cyan("Use the -h|-help argument to view the usage instructions.");
            Environment.Exit(-1);
        }

        public static void PrintHelp()
        {
            Cyan(@"
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
                                                When set to true, will output a diagnostics binlog file if using '-Direction ToTripleSlash'.

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

    -PortTypeSummaries          bool            Default is true (ports Type summaries).
                                                Enable or disable finding and porting Type summaries.
                                                    Usage example:
                                                        -PortTypeSummaries false

    -PortTypeTypeParams         bool            Default is true (ports Type TypeParams).
                                                Enable or disable finding and porting Type TypeParams.
                                                    Usage example:
                                                        -PortTypeTypeParams false

    -PrintSummaryDetails        bool            Default is false (does not print summary details).
                                                Prints the list of APIs that got modified by the tool.
                                                    Usage example:
                                                        -PrintSummaryDetails true

    -PrintUndoc                 bool            Default is false (prints a basic summary).
                                                Prints a detailed summary of all the docs APIs that are undocumented.
                                                    Usage example:
                                                        -PrintUndoc true

    -Save                       bool            Default is false (does not save changes).
                                                When using -Direction ToDocs, indicates whether you want to save the Docs xml file changes.
                                                When using -Direction ToTripleSlash, this parameter is always true, so don't specify it.
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
            Warning(@"
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
            Magenta(@"
    Note:
        If the assembly and the namespace is exactly the same, you can skip the -IncludedNamespaces argument.

            ");
        }
    }
}

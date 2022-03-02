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

    -BinLog                 bool                Default is false (binlog file generation is disabled).
                                                When set to true, will output a diagnostics binlog file if using '-Direction ToTripleSlash'.

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

      PortToTripleSlash
        -CsProj <pathToCsproj>
        -Docs <pathToDocsXmlFolder>
        -IncludedAssemblies <assembly1>[,<assembly2>,...,<assemblyN>]
        -IncludedNamespaces <namespace1>[,<namespace2>,...,<namespaceN>]


        Example:
            PortToTripleSlash \
                -Direction ToTripleSlash \
                -CsProj D:\runtime\src\libraries\System.IO.Compression.Brotli\src\System.IO.Compression.Brotli.csproj \
                -Docs D:\dotnet-api-docs\xml \
                -IncludedAssemblies System.IO.Compression.Brotli \
                -IncludedNamespaces System.IO.Compression \
");
            Magenta(@"
    Note:
        If the assembly and the namespace is exactly the same, you can skip the -IncludedNamespaces argument.

            ");
        }
    }
}

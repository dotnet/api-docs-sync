using System;

namespace DocsPortingTool
{
    public class Log
    {
        private static void WriteLine(string format, params object[] args)
        {
            if (args == null || args.Length == 0)
            {
                Console.WriteLine(format);
            }
            else
            {
                Console.WriteLine(format, args);
            }
        }

        private static void Write(string format, params object[] args)
        {
            if (args == null || args.Length == 0)
            {
                Console.Write(format);
            }
            else
            {
                Console.Write(format, args);
            }
        }

        public static void Print(bool endline, ConsoleColor foregroundColor, string format, params object[] args)
        {
            ConsoleColor initialColor = Console.ForegroundColor;
            Console.ForegroundColor = foregroundColor;
            if (endline)
            {
                WriteLine(format, args);
            }
            else
            {
                Write(format, args);
            }
            Console.ForegroundColor = initialColor;
        }

        public static void Info(string format)
        {
            Info(format, null);
        }

        public static void Info(string format, params object[] args)
        {
            Info(true, format, args);
        }

        public static void Info(bool endline, string format, params object[] args)
        {
            Print(endline, ConsoleColor.White, format, args);
        }

        public static void Success(string format)
        {
            Success(format, null);
        }

        public static void Success(string format, params object[] args)
        {
            Success(true, format, args);
        }

        public static void Success(bool endline, string format, params object[] args)
        {
            Print(endline, ConsoleColor.Green, format, args);
        }

        public static void Warning(string format)
        {
            Warning(format, null);
        }

        public static void Warning(string format, params object[] args)
        {
            Warning(true, format, args);
        }

        public static void Warning(bool endline, string format, params object[] args)
        {
            Print(endline, ConsoleColor.Yellow, format, args);
        }

        public static void Error(string format)
        {
            Error(format, null);
        }

        public static void Error(string format, params object[] args)
        {
            Error(true, format, args);
        }

        public static void Error(bool endline, string format, params object[] args)
        {
            Print(endline, ConsoleColor.Red, format, args);
        }

        public static void Working(string format)
        {
            Working(format, null);
        }

        public static void Working(string format, params object[] args)
        {
            Working(true, format, args);
        }

        public static void Working(bool endline, string format, params object[] args)
        {
            Print(endline, ConsoleColor.Cyan, format, args);
        }

        public static void Assert(bool condition, string format, params object[] args)
        {
            Assert(true, condition, format, args);
        }

        public static void Assert(bool endline, bool condition, string format, params object[] args)
        {
            if (condition)
            {
                Success(endline, format, args);
            }
            else
            {
                Error(endline, format, args);
            }
        }

        public static void Line()
        {
            Console.WriteLine();
        }

        public delegate void PrintHelpFunction();

        public static void LogErrorAndExit(string format, params object[] args)
        {
            Error(format, args);
            Environment.Exit(0);
        }

        public static void LogErrorPrintHelpAndExit(PrintHelpFunction helpFunction, string format, params object[] args)
        {
            Error(format, args);
            helpFunction();
            Environment.Exit(0);
        }

        public static void PrintHelp()
        {
            Working(@"
This tool finds and ports triple slash comments found in .NET repos but do not yet exist in the dotnet-api-docs repo.

Change %SourceRepos% to match the location of all your cloned git repos.

Options:

    no arguments:   -h or -help             Optional. Displays this help message. If used, nothing else will be processed.



    folder path:    -docs                   Mandatory. The absolute directory root path where your documentation xml files are located.

                                                Known locations:
                                                    > Runtime:   %SourceRepos%\dotnet-api-docs\xml
                                                    > WPF:       ? (TODO)
                                                    > WinForms:  ? (TODO)

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
                                                    > Runtime:   %SourceRepos%\runtime\artifacts\bin\
                                                    > WinForms:  %SourceRepos%\winforms\artifacts\bin\
                                                    > WPF:       %SourceRepos%\wpf\.tools\native\bin\dotnet-api-docs_netcoreapp3.0\0.0.0.1\_intellisense\\netcore-3.0\

                                                Usage example:
                                                    -tripleslash %SourceRepos%\corefx\artifacts\bin\

            ");
        }
    }
}
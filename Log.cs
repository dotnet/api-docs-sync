using System;

namespace DocsPortingTool
{
    class Log
    {
        public static void Print(bool endline, ConsoleColor foregroundColor, string format, params object[] args)
        {
            ConsoleColor initialColor = Console.ForegroundColor;
            Console.ForegroundColor = foregroundColor;
            if (endline)
            {
                Console.WriteLine(format, args);
            }
            else
            {
                Console.Write(format, args);
            }
            Console.ForegroundColor = initialColor;
        }

        public static void Info(string format, params object[] args)
        {
            Info(true, format, args);
        }

        public static void Info(bool endline, string format, params object[] args)
        {
            Print(endline, ConsoleColor.White, format, args);
        }

        public static void Success(string format, params object[] args)
        {
            Success(true, format, args);
        }

        public static void Success(bool endline, string format, params object[] args)
        {
            Print(endline, ConsoleColor.Green, format, args);
        }
        public static void Warning(string format, params object[] args)
        {
            Warning(true, format, args);
        }

        public static void Warning(bool endline, string format, params object[] args)
        {
            Print(endline, ConsoleColor.Yellow, format, args);
        }

        public static void Error(string format, params object[] args)
        {
            Error(true, format, args);
        }

        public static void Error(bool endline, string format, params object[] args)
        {
            Print(endline, ConsoleColor.Red, format, args);
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

        public static void LogErrorAndExit(string format, params object[] args)
        {
            Error(format, args);
            Environment.Exit(0);
        }

        public static void LogErrorPrintHelpAndExit(string format, params object[] args)
        {
            Error(format, args);
            PrintHelp();
            Environment.Exit(0);
        }

        public static void PrintHelp()
        {
            Print(true, ConsoleColor.Cyan, @"
This tool finds and ports triple slash comments found in .NET repos but do not yet exist in the dotnet-api-docs repo.

Options:

    no arguments:   -h or -help             Optional. Displays this help message. If used, nothing else will be processed.


    folder path:    -docs                   Mandatory. The absolute directory path to the Docs repo.

                                                Usage example:
                                                    -docs %SourceRepos%\dotnet-api-docs


    string list:    -exclude                Optional. Comma separated list (no spaces) of specific .NET assemblies to ignore. Default is empty.
                                                Usage example:
                                                    -exclude System.IO.Compression,System.IO.Pipes


    string:         -include                Mandatory. Comma separated list (no spaces) of assemblies to include.

                                                Usage example:
                                                    System.IO,System.Runtime.Intrinsics


    bool:           -save                   Optional. Wether we want to save the changes in the dotnet-api-docs xml files. Default is false.
                                                Usage example:
                                                    -save true


    folder paths:   -tripleslash            Mandatory. List of absolute directory paths (comma separated) where we should look for triple slash comment xml files.

                                                Known locations:
                                                    > CoreCLR:   coreclr\bin\Product\Windows_NT.x64.Debug\IL\
                                                    > CoreFX:    corefx\artifacts\bin\
                                                    > WinForms:  winforms\artifacts\bin\
                                                    > WPF:       wpf\.tools\native\bin\dotnet-api-docs_netcoreapp3.0\0.0.0.1\_intellisense\\netcore-3.0\

                                                Usage example:
                                                    -tripleslash %SourceRepos%\corefx\artifacts\bin\,%SourceRepos%\coreclr\bin\Product\Windows_NT.x64.Debug\IL\

            ");
        }
    }
}
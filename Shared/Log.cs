using System;

namespace Shared
{
    public class Log
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

        public static void Working(string format, params object[] args)
        {
            Working(true, format, args);
        }

        public static void Working(bool endline, string format, params object[] args)
        {
            Print(endline, ConsoleColor.Cyan, format, args);
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
    }
}
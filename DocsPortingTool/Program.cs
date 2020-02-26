using DocsPortingTool;

namespace Program
{
    class Program
    {
        public static void Main(string[] args)
        {
            Configuration.GetFromCommandLineArguments(args);
            DocsPortingTool.DocsPortingTool.Start();
        }
    }
}

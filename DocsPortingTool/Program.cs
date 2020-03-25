namespace DocsPortingTool
{
    class DocsPortingTool
    {
        public static void Main(string[] args)
        {
            Configuration config = Configuration.GetFromCommandLineArguments(args);
            Analyzer analyzer = new Analyzer(config);
            analyzer.Start();
        }
    }
}

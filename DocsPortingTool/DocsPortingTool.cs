namespace DocsPortingTool
{
    public static class DocsPortingTool
    {
        public static void Main(string[] args)
        {
            Configuration config = Configuration.GetFromCommandLineArguments(args);
            Analyzer analyzer = new Analyzer(config);
            analyzer.Start();
        }
    }
}

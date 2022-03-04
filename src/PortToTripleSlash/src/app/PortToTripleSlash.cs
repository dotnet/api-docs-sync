using DocsPortingTool.Libraries;

namespace DocsPortingTool
{
    class PortToTripleSlash
    {
        public static void Main(string[] args)
        {
            Configuration config = Configuration.GetCLIArguments(args);
            ToTripleSlashPorter.Start(config);
        }
    }
}

using DocsPortingTool.Libraries;

namespace DocsPortingTool
{
    class PortToDocs
    {
        public static void Main(string[] args)
        {
            Configuration config = Configuration.GetCLIArguments(args);
            ToDocsPorter porter = new(config);
            porter.Start();
        }
    }
}

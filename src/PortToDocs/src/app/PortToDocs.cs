using ApiDocsSync.Libraries;

namespace ApiDocsSync
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

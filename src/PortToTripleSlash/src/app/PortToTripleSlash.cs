using ApiDocsSync.Libraries;

namespace ApiDocsSync
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

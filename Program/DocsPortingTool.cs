#nullable enable
using Libraries;
using System;
using System.Threading.Tasks;

namespace DocsPortingTool
{
    class DocsPortingTool
    {
        public static void Main(string[] args)
        {
            Configuration config = Configuration.GetCLIArgumentsForDocsPortingTool(args);
            switch (config.Direction)
            {
                case Configuration.PortingDirection.ToDocs:
                    {
                        ToDocsPorter porter = new(config);
                        porter.Start();
                        break;
                    }
                case Configuration.PortingDirection.ToTripleSlash:
                    {
                        ToTripleSlashPorter.Start(config);
                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException($"Unrecognized porting direction: {config.Direction}");
            }
        }
    }
}

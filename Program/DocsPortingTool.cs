#nullable enable
using Libraries;
using System;

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
                        ToTripleSlashPorter porter = new(config);
                        porter.Start();
                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException($"Unrecognized porting direction: {config.Direction}");
            }
        }
    }
}

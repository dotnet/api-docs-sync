#nullable enable
using Libraries;
using System;

namespace DocsPortingTool
{
    class DocsPortingTool
    {
        public static async Task Main(string[] args)
        {
            Task loggingTask = Log.StartAsync();
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

            Log.Finished();
            await loggingTask;
        }
    }
}

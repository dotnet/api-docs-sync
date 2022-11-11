// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ApiDocsSync.PortToDocs
{
    class PortToDocs
    {
        public static void Main(string[] args)
        {
            Configuration config = Configuration.GetCLIArguments(args);
            ToDocsPorter porter = new(config);
            porter.CollectFiles();
            porter.Start();
            porter.SaveToDisk();
            porter.PrintSummary();
        }
    }
}

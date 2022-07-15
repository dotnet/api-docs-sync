// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

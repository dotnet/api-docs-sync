// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using ApiDocsSync.Libraries;

namespace ApiDocsSync
{
    class PortToTripleSlash
    {
        public static async Task Main(string[] args)
        {
            Configuration config = Configuration.GetCLIArguments(args);

            VSLoader.LoadVSInstance();

            CancellationTokenSource cts = new();
            config.Loader = new MSBuildLoader(config.BinLogPath);
            await config.Loader.LoadMainProjectAsync(config.CsProj, config.IsMono, cts.Token).ConfigureAwait(false);

            ToTripleSlashPorter porter = new(config);
            await porter.StartAsync(cts.Token).ConfigureAwait(false);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit.Abstractions;

namespace ApiDocsSync.Libraries.Tests
{
    public abstract class BasePortTests
    {
        protected ITestOutputHelper Output { get; private set; }

        public BasePortTests(ITestOutputHelper output) => Output = output;
    }
}

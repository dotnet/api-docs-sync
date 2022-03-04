using Xunit.Abstractions;

namespace ApiDocsSync.Libraries.Tests
{
    public abstract class BasePortTests
    {
        protected ITestOutputHelper Output { get; private set; }

        public BasePortTests(ITestOutputHelper output) => Output = output;
    }
}

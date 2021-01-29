using Xunit.Abstractions;

namespace Libraries.Tests
{
    public abstract class BasePortTests
    {
        protected ITestOutputHelper Output { get; private set; }

        public BasePortTests(ITestOutputHelper output) => Output = output;
    }
}

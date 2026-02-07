using Xunit;

namespace RiotProxy.Tests
{
    /// <summary>
    /// Test collection that disables parallelization to avoid environment-variable race conditions.
    /// </summary>
    [CollectionDefinition("EnvIsolation", DisableParallelization = true)]
    public class EnvIsolationCollection
    {
        // No fixtures required; the attribute controls parallelization.
    }
}

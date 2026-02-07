using System;
using System.Collections.Generic;

namespace RiotProxy.Tests
{
    /// <summary>
    /// Temporarily sets environment variables for the duration of a scope and restores originals on dispose.
    /// Helps avoid polluting process-wide state across tests.
    /// </summary>
    public sealed class EnvironmentVariableScope : IDisposable
    {
        private readonly Dictionary<string, string?> _originals;

        private EnvironmentVariableScope(Dictionary<string, string?> originals)
        {
            _originals = originals;
        }

        public static EnvironmentVariableScope Set(params (string key, string? value)[] variables)
        {
            var originals = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var (key, value) in variables)
            {
                originals[key] = Environment.GetEnvironmentVariable(key);
                Environment.SetEnvironmentVariable(key, value);
            }
            return new EnvironmentVariableScope(originals);
        }

        public void Dispose()
        {
            foreach (var kv in _originals)
            {
                Environment.SetEnvironmentVariable(kv.Key, kv.Value);
            }
        }
    }
}

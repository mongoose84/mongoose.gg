
using System.Reflection;

namespace RiotProxy.Utilities
{
    /// <summary>
    /// Provides a static, read‑only view oof the metrics
    /// </summary>
    public static class Metrics
    {
        // Endpoint counters
        private static long _homeHits;
        private static long _metricHits;
        private static long _winrateHits;
        private static long _summonerHits;
        private static string _lastUrlCalled = string.Empty;
        public static void IncrementHome() => Interlocked.Increment(ref _homeHits);
        public static void IncrementMetrics() => Interlocked.Increment(ref _metricHits);
        public static void IncrementWinrate() => Interlocked.Increment(ref _winrateHits);
        public static void IncrementSummoner() => Interlocked.Increment(ref _summonerHits);
        public static void SetLastUrlCalled(string url)
            => Interlocked.Exchange(ref _lastUrlCalled, url ?? string.Empty);

        // Expose the counters
        public static long HomeHits => Interlocked.Read(ref _homeHits);
        public static long MetricHits => Interlocked.Read(ref _metricHits);
        public static long WinrateHits => Interlocked.Read(ref _winrateHits);
        public static long SummonerHits => Interlocked.Read(ref _summonerHits);
        public static string LastUrlCalled => Volatile.Read(ref _lastUrlCalled);
        public static string BuildNumber =>
            typeof(Metrics).Assembly
                        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                        ?.InformationalVersion ?? "0";

        // Helper for the /metrics endpoint
        public static string GetMetricsJson() =>
            $@"{{   ""build"": ""{BuildNumber}"", 
                    ""homeHits"": {HomeHits}, 
                    ""metricHits"": {MetricHits},
                    ""winrateHits"": {WinrateHits},
                    ""summonerHits"": {SummonerHits},
                    ""lastUrlCalled"": ""{_lastUrlCalled}""
                }}";
                    

    }
}
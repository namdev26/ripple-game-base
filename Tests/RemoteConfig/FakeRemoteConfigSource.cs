using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RippleGame.RemoteConfig.Tests
{
    /// <summary>Source double with a configurable fetch result, delay and values.</summary>
    internal sealed class FakeRemoteConfigSource : IRemoteConfigSource
    {
        /// <summary>Results handed out per fetch call. The last one repeats.</summary>
        public Queue<bool> Results { get; } = new Queue<bool>();

        public bool Result { get; set; } = true;
        public TimeSpan Delay { get; set; } = TimeSpan.Zero;
        public bool ThrowOnFetch { get; set; }
        public Dictionary<string, string> Values { get; } = new Dictionary<string, string>();

        /// <summary>Copied into <see cref="Values"/> when a fetch completes, like a real activation.</summary>
        public Dictionary<string, string> ValuesAfterFetch { get; } = new Dictionary<string, string>();
        public IReadOnlyDictionary<string, string> Defaults { get; private set; }
        public int FetchCount { get; private set; }
        public TimeSpan LastTimeout { get; private set; }
        public TimeSpan LastMinimumInterval { get; private set; }

        public async Task<bool> FetchAndActivateAsync(TimeSpan timeout, TimeSpan minimumInterval)
        {
            FetchCount++;
            LastTimeout = timeout;
            LastMinimumInterval = minimumInterval;
            var result = Results.Count > 0 ? Results.Dequeue() : Result;
            if (Delay > TimeSpan.Zero) await Task.Delay(Delay);
            if (ThrowOnFetch) throw new InvalidOperationException("fetch failed");
            foreach (var pair in ValuesAfterFetch) Values[pair.Key] = pair.Value;
            return result;
        }

        public bool TryGetString(string key, out string value) => Values.TryGetValue(key, out value);

        public void SetDefaults(IReadOnlyDictionary<string, string> defaults) => Defaults = defaults;
    }
}

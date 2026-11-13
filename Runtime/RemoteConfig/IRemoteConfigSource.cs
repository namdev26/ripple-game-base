using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RippleGame.RemoteConfig
{
    /// <summary>
    /// A Remote Config backend. SDK-gated assemblies register one with <c>RemoteConfig.RegisterSource</c>.
    /// Implementations must never throw from these members; a failure is reported as <c>false</c>.
    /// </summary>
    public interface IRemoteConfigSource
    {
        /// <summary>
        /// Fetches new values and activates them. Resolves true when a fetch succeeded and false on failure.
        /// <paramref name="minimumInterval"/> is the cache lifetime of the last fetch. The core stops waiting after
        /// <paramref name="timeout"/> but keeps observing the task: one that resolves true later still raises Activated.
        /// </summary>
        Task<bool> FetchAndActivateAsync(TimeSpan timeout, TimeSpan minimumInterval);

        /// <summary>Reads the raw string of a key from fetched, activated or default values. False when the source has no value.</summary>
        bool TryGetString(string key, out string value);

        /// <summary>Hands the in-app defaults to the source. Called once before the first fetch.</summary>
        void SetDefaults(IReadOnlyDictionary<string, string> defaults);
    }
}

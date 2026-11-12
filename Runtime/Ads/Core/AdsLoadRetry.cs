using System;

namespace RippleGame.Ads
{
    /// <summary>
    /// Delay before reloading a format after a failed load. The first <c>maxAttempts</c> retries back off exponentially
    /// from <c>initialDelaySeconds</c>; every later retry waits <c>maxDelaySeconds</c>. Retrying never stops on its own,
    /// so a format is not left empty for the rest of the session. Providers reset the attempt on a successful load.
    /// </summary>
    internal static class AdsLoadRetry
    {
        private const float MinimumDelaySeconds = 0.1f;

        /// <param name="attempt">1 for the first retry after a failure.</param>
        internal static float DelaySeconds(int attempt, int maxAttempts, float initialDelaySeconds, float maxDelaySeconds)
        {
            var max = Math.Max(MinimumDelaySeconds, maxDelaySeconds);
            if (attempt > maxAttempts)
            {
                return max;
            }

            var exponent = Math.Min(Math.Max(attempt - 1, 0), 30);
            var delay = Math.Max(MinimumDelaySeconds, initialDelaySeconds) * Math.Pow(2, exponent);
            return (float)Math.Min(delay, max);
        }
    }
}

using System;

namespace RippleGame.Ads
{
    /// <summary>
    /// Converts AppLovin MAX impression revenue (USD as a double) into the SDK-free <see cref="AdRevenue"/>.
    /// Lives in core so it can be tested without the MAX package installed.
    /// </summary>
    internal static class MaxRevenueConversion
    {
        internal const string Currency = "USD";

        /// <summary>
        /// MAX reports -1 when the revenue could not be determined. Negative, NaN and infinite values are not
        /// revenue, so no Paid event is raised for them. 0 is a real (zero) impression and is reported.
        /// </summary>
        internal static bool ShouldReport(double usd)
        {
            return !double.IsNaN(usd) && !double.IsInfinity(usd) && usd >= 0;
        }

        /// <summary>USD to micros, rounded half away from zero. Negative, NaN and infinite values become 0.</summary>
        internal static long ToMicros(double usd)
        {
            if (double.IsNaN(usd) || double.IsInfinity(usd) || usd <= 0)
            {
                return 0;
            }

            return (long)Math.Round(usd * 1_000_000, MidpointRounding.AwayFromZero);
        }

        internal static AdRevenue Create(double usd, string adUnitId, string networkName)
        {
            return new AdRevenue(ToMicros(usd), Currency, 0, adUnitId, networkName, AdRevenue.PlatformMax);
        }
    }
}

namespace RippleGame.Ads.Max
{
    /// <summary>Converts a MAX revenue-paid callback into the SDK-free <see cref="AdRevenue"/>.</summary>
    internal static class MaxRevenue
    {
        /// <summary>
        /// False when the callback carries no usable revenue: no ad info, or MAX's error value (-1) or NaN.
        /// Such callbacks raise no Paid event.
        /// </summary>
        internal static bool TryFrom(string adUnitId, MaxSdkBase.AdInfo info, out AdRevenue revenue)
        {
            if (info == null || !MaxRevenueConversion.ShouldReport(info.Revenue))
            {
                revenue = default;
                return false;
            }

            var unitId = string.IsNullOrEmpty(info.AdUnitIdentifier) ? adUnitId : info.AdUnitIdentifier;
            revenue = MaxRevenueConversion.Create(info.Revenue, unitId, info.NetworkName);
            return true;
        }

        internal static string Describe(MaxSdkBase.ErrorInfo error)
        {
            return error == null ? "Unknown MAX error." : $"{error.Code}: {error.Message}";
        }
    }
}

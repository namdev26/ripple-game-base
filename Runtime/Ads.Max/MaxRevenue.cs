namespace RippleGame.Ads.Max
{
    /// <summary>Converts a MAX revenue-paid callback into the SDK-free <see cref="AdRevenue"/>.</summary>
    internal static class MaxRevenue
    {
        internal static AdRevenue From(string adUnitId, MaxSdkBase.AdInfo info)
        {
            if (info == null)
            {
                return MaxRevenueConversion.Create(0, adUnitId, null);
            }

            var unitId = string.IsNullOrEmpty(info.AdUnitIdentifier) ? adUnitId : info.AdUnitIdentifier;
            return MaxRevenueConversion.Create(info.Revenue, unitId, info.NetworkName);
        }

        internal static string Describe(MaxSdkBase.ErrorInfo error)
        {
            return error == null ? "Unknown MAX error." : $"{error.Code}: {error.Message}";
        }
    }
}

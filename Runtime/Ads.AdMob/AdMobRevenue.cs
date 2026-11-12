using System;
using GoogleMobileAds.Api;

namespace RippleGame.Ads.AdMob
{
    /// <summary>Converts the Google Mobile Ads paid event into the SDK-free <see cref="AdRevenue"/>.</summary>
    internal static class AdMobRevenue
    {
        internal static AdRevenue From(AdValue value, ResponseInfo info, string unitId)
        {
            string adSourceName = null;
            try
            {
                adSourceName = info?.GetLoadedAdapterResponseInfo()?.AdSourceName;
            }
            catch (Exception)
            {
                // Response info is optional metadata; revenue is still reported without it.
            }

            return new AdRevenue(value.Value, value.CurrencyCode, (int)value.Precision, unitId, adSourceName, AdRevenue.PlatformAdMob);
        }
    }
}

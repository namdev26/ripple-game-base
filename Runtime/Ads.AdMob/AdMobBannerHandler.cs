using System;
using GoogleMobileAds.Api;

namespace RippleGame.Ads.AdMob
{
    internal sealed class AdMobBannerHandler : IDisposable
    {
        private readonly string _unitId;
        private BannerView _banner;

        internal AdMobBannerHandler(string unitId)
        {
            _unitId = unitId;
        }

        internal event Action Loaded;
        internal event Action<string> LoadFailed;
        internal event Action<AdRevenue> Paid;

        internal void Show(BannerPosition position)
        {
            if (_banner == null)
            {
                var adPosition = position == BannerPosition.Top ? AdPosition.Top : AdPosition.Bottom;
                var size = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);
                _banner = new BannerView(_unitId, size, adPosition);
                _banner.OnBannerAdLoaded += () => MainThreadDispatcher.Post(() => Loaded?.Invoke());
                _banner.OnBannerAdLoadFailed += error => MainThreadDispatcher.Post(() => LoadFailed?.Invoke(error.ToString()));
                var banner = _banner;
                _banner.OnAdPaid += value =>
                {
                    var revenue = AdMobRevenue.From(value, banner.GetResponseInfo(), _unitId);
                    MainThreadDispatcher.Post(() => Paid?.Invoke(revenue));
                };
                _banner.LoadAd(new AdRequest());
            }

            _banner.Show();
        }

        internal void Hide() => _banner?.Hide();

        internal void Destroy()
        {
            _banner?.Destroy();
            _banner = null;
        }

        public void Dispose() => Destroy();
    }
}

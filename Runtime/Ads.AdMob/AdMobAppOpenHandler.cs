using System;
using GoogleMobileAds.Api;

namespace RippleGame.Ads.AdMob
{
    internal sealed class AdMobAppOpenHandler : IDisposable
    {
        private readonly string _unitId;
        private AppOpenAd _ad;

        internal AdMobAppOpenHandler(string unitId) => _unitId = unitId;

        internal event Action Loaded;
        internal event Action<string> LoadFailed;
        internal event Action<AdRevenue> Paid;
        internal bool IsReady => _ad != null && _ad.CanShowAd();

        internal void Load()
        {
            DestroyAd();
            AppOpenAd.Load(_unitId, new AdRequest(), (ad, error) => MainThreadDispatcher.Post(() =>
            {
                if (error != null || ad == null)
                {
                    LoadFailed?.Invoke(error?.ToString() ?? "App open returned no ad.");
                    return;
                }

                _ad = ad;
                ad.OnAdPaid += value =>
                {
                    var revenue = AdMobRevenue.From(value, ad.GetResponseInfo(), _unitId);
                    MainThreadDispatcher.Post(() => Paid?.Invoke(revenue));
                };
                Loaded?.Invoke();
            }));
        }

        internal bool Show(Action opened, Action closed, Action<string> failed)
        {
            if (!IsReady)
            {
                return false;
            }

            var ad = _ad;
            _ad = null;
            ad.OnAdFullScreenContentOpened += () => MainThreadDispatcher.Post(opened);
            ad.OnAdFullScreenContentClosed += () => MainThreadDispatcher.Post(() =>
            {
                ad.Destroy();
                closed?.Invoke();
            });
            ad.OnAdFullScreenContentFailed += error => MainThreadDispatcher.Post(() =>
            {
                ad.Destroy();
                failed?.Invoke(error.ToString());
            });
            ad.Show();
            return true;
        }

        public void Dispose() => DestroyAd();

        private void DestroyAd()
        {
            _ad?.Destroy();
            _ad = null;
        }
    }
}

using System;
using System.Threading.Tasks;

namespace RippleGame.Ads.Max
{
    internal sealed class MaxRewardedHandler : IDisposable
    {
        // Mediated networks do not guarantee whether the reward or the hidden callback arrives first.
        private const int RewardGraceMilliseconds = 1000;

        private readonly string _unitId;
        private bool _loading;
        private bool _showing;
        private bool _didReward;
        private bool _waitingForReward;
        private int _showVersion;
        private Action _opened;
        private Action _rewardEarned;
        private Action _closed;
        private Action<string> _failed;

        internal MaxRewardedHandler(string unitId)
        {
            _unitId = unitId;
            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnAdLoaded;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnAdLoadFailed;
            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += OnAdDisplayed;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnAdDisplayFailed;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnAdReceivedReward;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnAdHidden;
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnAdRevenuePaid;
        }

        internal event Action Loaded;
        internal event Action<string> LoadFailed;
        internal event Action<AdRevenue> Paid;
        internal bool IsReady => MaxSdk.IsRewardedAdReady(_unitId);
        internal bool IsLoading => _loading;

        internal void Load()
        {
            if (_loading || IsReady)
            {
                return;
            }

            _loading = true;
            MaxSdk.LoadRewardedAd(_unitId);
        }

        /// <summary>
        /// Returns false when no ad is ready; otherwise exactly one of closed or failed runs later, and
        /// rewardEarned runs at most once before closed.
        /// </summary>
        internal bool Show(Action opened, Action rewardEarned, Action closed, Action<string> failed)
        {
            if (_showing || !IsReady)
            {
                return false;
            }

            _showing = true;
            _didReward = false;
            _waitingForReward = false;
            _showVersion++;
            _opened = opened;
            _rewardEarned = rewardEarned;
            _closed = closed;
            _failed = failed;
            MaxSdk.ShowRewardedAd(_unitId);
            return true;
        }

        public void Dispose()
        {
            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent -= OnAdLoaded;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent -= OnAdLoadFailed;
            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent -= OnAdDisplayed;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent -= OnAdDisplayFailed;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent -= OnAdReceivedReward;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent -= OnAdHidden;
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent -= OnAdRevenuePaid;
            if (_showing)
            {
                CompleteFailed("The ads service was disposed while the rewarded ad was showing.");
            }
        }

        private void OnAdLoaded(string unitId, MaxSdkBase.AdInfo info)
        {
            if (unitId != _unitId) return;
            MainThreadDispatcher.Post(() =>
            {
                _loading = false;
                Loaded?.Invoke();
            });
        }

        private void OnAdLoadFailed(string unitId, MaxSdkBase.ErrorInfo error)
        {
            if (unitId != _unitId) return;
            var message = MaxRevenue.Describe(error);
            MainThreadDispatcher.Post(() =>
            {
                _loading = false;
                LoadFailed?.Invoke(message);
            });
        }

        private void OnAdDisplayed(string unitId, MaxSdkBase.AdInfo info)
        {
            if (unitId != _unitId) return;
            MainThreadDispatcher.Post(() =>
            {
                var opened = _opened;
                _opened = null;
                opened?.Invoke();
            });
        }

        private void OnAdDisplayFailed(string unitId, MaxSdkBase.ErrorInfo error, MaxSdkBase.AdInfo info)
        {
            if (unitId != _unitId) return;
            var message = MaxRevenue.Describe(error);
            MainThreadDispatcher.Post(() =>
            {
                if (_showing)
                {
                    CompleteFailed(message);
                }
            });
        }

        private void OnAdReceivedReward(string unitId, MaxSdkBase.Reward reward, MaxSdkBase.AdInfo info)
        {
            if (unitId != _unitId) return;
            MainThreadDispatcher.Post(() =>
            {
                if (!_showing || _didReward)
                {
                    return;
                }

                _didReward = true;
                var rewardEarned = _rewardEarned;
                _rewardEarned = null;
                rewardEarned?.Invoke();
                if (_waitingForReward)
                {
                    CompleteClosed();
                }
            });
        }

        private void OnAdHidden(string unitId, MaxSdkBase.AdInfo info)
        {
            if (unitId != _unitId) return;
            MainThreadDispatcher.Post(() =>
            {
                if (!_showing || _waitingForReward)
                {
                    return;
                }

                if (_didReward)
                {
                    CompleteClosed();
                    return;
                }

                // Give a late reward callback a short window before closing without a reward.
                _waitingForReward = true;
                var version = _showVersion;
                Task.Delay(RewardGraceMilliseconds).ContinueWith(_ => MainThreadDispatcher.Post(() =>
                {
                    if (_showing && _showVersion == version)
                    {
                        CompleteClosed();
                    }
                }));
            });
        }

        private void OnAdRevenuePaid(string unitId, MaxSdkBase.AdInfo info)
        {
            if (unitId != _unitId) return;
            if (!MaxRevenue.TryFrom(_unitId, info, out var revenue)) return;
            MainThreadDispatcher.Post(() => Paid?.Invoke(revenue));
        }

        private void CompleteClosed()
        {
            var closed = _closed;
            ClearShowState();
            closed?.Invoke();
        }

        private void CompleteFailed(string message)
        {
            var failed = _failed;
            ClearShowState();
            failed?.Invoke(message);
        }

        private void ClearShowState()
        {
            _showing = false;
            _waitingForReward = false;
            _opened = null;
            _rewardEarned = null;
            _closed = null;
            _failed = null;
        }
    }
}

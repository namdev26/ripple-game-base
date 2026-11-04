using System;
using RippleGame.Economy;
using RippleGame.Progression;

namespace RippleGame.Flow
{
    public sealed class LevelFlowConfig
    {
        public ILevelGameplay Gameplay;
        public ILevelFlowView View;
        public LevelProgress Progress;
        /// <summary>Levels are numbered continuously from 1 to this value.</summary>
        public int LevelCount;
        /// <summary>Writes pending data, for example <c>() => save.Flush()</c>. Called before any popup appears.</summary>
        public Action Commit;
        public Action ExitToHome;

        /// <summary>Optional. Without it there is no revive, no x2 and no interstitial.</summary>
        public ILevelFlowAds Ads;
        /// <summary>Optional.</summary>
        public ILevelFlowEvents Events;
        /// <summary>Optional, together with <see cref="RewardPolicy"/>, for games without currency.</summary>
        public CurrencyWallet Wallet;
        /// <summary>Optional, together with <see cref="Wallet"/>.</summary>
        public Func<LevelOutcome, int> RewardPolicy;
    }

    /// <summary>Runs a level from start to result: progress, rewards, revive, ads and analytics order.</summary>
    /// <remarks>Dispose it when its scene unloads so late ad callbacks cannot touch destroyed objects.</remarks>
    public sealed class LevelFlow : IDisposable
    {
        private readonly LevelFlowConfig config;
        private LevelOutcome lastOutcome;
        // Every state change invalidates earlier offers and ad callbacks.
        private int generation;
        private bool playing;
        private bool reviveUsed;
        private bool rewardedShowing;
        private bool disposed;

        public int CurrentLevel { get; private set; }

        public LevelFlow(LevelFlowConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (config.Gameplay == null) throw new ArgumentException("Gameplay is required.", nameof(config));
            if (config.View == null) throw new ArgumentException("View is required.", nameof(config));
            if (config.Progress == null) throw new ArgumentException("Progress is required.", nameof(config));
            if (config.Commit == null) throw new ArgumentException("Commit is required.", nameof(config));
            if (config.ExitToHome == null) throw new ArgumentException("ExitToHome is required.", nameof(config));
            if (config.LevelCount < 1) throw new ArgumentException("LevelCount must be at least 1.", nameof(config));
            if ((config.Wallet == null) != (config.RewardPolicy == null))
                throw new ArgumentException("Wallet and RewardPolicy must be set together.", nameof(config));

            this.config = config;
            config.Gameplay.Finished += OnFinished;
        }

        public void Start(int level)
        {
            if (disposed) throw new ObjectDisposedException(nameof(LevelFlow));
            if (level < 1 || level > config.LevelCount)
                throw new ArgumentOutOfRangeException(nameof(level), $"Level must be between 1 and {config.LevelCount}.");
            Begin(level);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            playing = false;
            generation++;
            config.Gameplay.Finished -= OnFinished;
        }

        private void Begin(int level)
        {
            generation++;
            rewardedShowing = false;
            reviveUsed = false;
            CurrentLevel = level;
            playing = true;
            config.Events?.LevelStarted(level);
            config.Gameplay.Begin(level);
        }

        private void OnFinished(LevelOutcome outcome)
        {
            if (disposed || !playing) return;
            playing = false;
            generation++;
            lastOutcome = outcome;

            if (outcome.Won) HandleWin(outcome);
            else if (!reviveUsed && config.Ads != null && config.Ads.IsRewardedReady) config.View.ShowRevive(new ReviveOffer(this, generation));
            else ShowLose();
        }

        private void HandleWin(LevelOutcome outcome)
        {
            config.Progress.MarkCompleted(CurrentLevel);

            var reward = 0;
            if (config.Wallet != null)
            {
                reward = Math.Max(0, config.RewardPolicy(outcome));
                config.Wallet.TryAdd(reward);
            }

            config.Commit();
            config.Events?.LevelCompleted(CurrentLevel, outcome.Score);
            config.View.ShowWin(new WinOffer(this, generation, CurrentLevel, outcome.Score, reward));
        }

        private void ShowLose()
        {
            config.Events?.LevelFailed(CurrentLevel, lastOutcome.FailReason);
            config.View.ShowLose(new LoseOffer(this, generation));
        }

        private void Advance()
        {
            if (CurrentLevel < config.LevelCount) Begin(CurrentLevel + 1);
            else config.ExitToHome();
        }

        private bool Accepts(int offerGeneration) => !disposed && !rewardedShowing && offerGeneration == generation;

        internal bool CanDouble(int offerGeneration, int reward) =>
            Accepts(offerGeneration) && reward > 0 && config.Ads != null && config.Ads.IsRewardedReady;

        internal void AcceptNext(int offerGeneration)
        {
            if (!Accepts(offerGeneration)) return;
            var token = ++generation;
            config.View.HideAll();

            if (config.Ads == null)
            {
                Advance();
                return;
            }

            config.Ads.ShowInterstitial(() =>
            {
                if (disposed || token != generation) return;
                generation++;
                Advance();
            });
        }

        internal void AcceptDouble(int offerGeneration, int reward, Action<bool> completed)
        {
            if (!CanDouble(offerGeneration, reward))
            {
                completed?.Invoke(false);
                return;
            }

            ShowRewarded(LevelAdPurpose.DoubleReward, offerGeneration, completed, () =>
            {
                config.Wallet.TryAdd(reward);
                config.Commit();
                config.View.HideAll();
                completed?.Invoke(true);
                Advance();
            });
        }

        internal void AcceptWatch(int offerGeneration, Action<bool> completed)
        {
            if (!Accepts(offerGeneration) || config.Ads == null)
            {
                completed?.Invoke(false);
                return;
            }

            ShowRewarded(LevelAdPurpose.Revive, offerGeneration, completed, () =>
            {
                reviveUsed = true;
                config.View.HideAll();
                completed?.Invoke(true);

                playing = true;
                if (!config.Gameplay.TryRevive()) Begin(CurrentLevel);
            });
        }

        internal void AcceptDecline(int offerGeneration)
        {
            if (!Accepts(offerGeneration)) return;
            generation++;
            config.View.HideAll();
            ShowLose();
        }

        internal void AcceptRetry(int offerGeneration)
        {
            if (!Accepts(offerGeneration)) return;
            generation++;
            config.View.HideAll();
            Begin(CurrentLevel);
        }

        internal void AcceptHome(int offerGeneration)
        {
            if (!Accepts(offerGeneration)) return;
            generation++;
            config.View.HideAll();
            config.ExitToHome();
        }

        private void ShowRewarded(LevelAdPurpose purpose, int offerGeneration, Action<bool> completed, Action onEarned)
        {
            rewardedShowing = true;
            config.Ads.ShowRewarded(purpose, earned =>
            {
                if (disposed || offerGeneration != generation) return;
                rewardedShowing = false;

                if (!earned)
                {
                    completed?.Invoke(false);
                    return;
                }

                generation++;
                onEarned();
            });
        }
    }
}

using System;
using System.Collections.Generic;
using NUnit.Framework;
using RippleGame.Economy;
using RippleGame.Flow;
using RippleGame.Progression;

namespace RippleGame.Base.Tests
{
    public sealed class LevelFlowTests
    {
        private sealed class Log : List<string>
        {
            public string Text => string.Join(",", this);
        }

        private sealed class FakeGameplay : ILevelGameplay
        {
            private readonly Log log;
            public bool ReviveResult = true;
            public int Subscribers => Finished?.GetInvocationList().Length ?? 0;
            public event Action<LevelOutcome> Finished;
            public FakeGameplay(Log log) => this.log = log;
            public void Begin(int level) => log.Add("begin" + level);
            public bool TryRevive()
            {
                log.Add("revive");
                return ReviveResult;
            }
            public void Win(int score = 3) => Finished?.Invoke(LevelOutcome.Win(score));
            public void Lose(string reason = "hit") => Finished?.Invoke(LevelOutcome.Lose(0, reason));
        }

        private sealed class FakeView : ILevelFlowView
        {
            private readonly Log log;
            public WinOffer Win;
            public ReviveOffer Revive;
            public LoseOffer Lose;
            public FakeView(Log log) => this.log = log;
            public void ShowWin(WinOffer offer)
            {
                Win = offer;
                log.Add("showWin");
            }
            public void ShowRevive(ReviveOffer offer)
            {
                Revive = offer;
                log.Add("showRevive");
            }
            public void ShowLose(LoseOffer offer)
            {
                Lose = offer;
                log.Add("showLose");
            }
            public void HideAll() => log.Add("hide");
        }

        private sealed class FakeAds : ILevelFlowAds
        {
            private readonly Log log;
            public bool Ready = true;
            public Action<bool> PendingRewarded;
            public LevelAdPurpose LastPurpose;
            public Action PendingInterstitial;
            public FakeAds(Log log) => this.log = log;
            public bool IsRewardedReady => Ready;
            public void ShowRewarded(LevelAdPurpose purpose, Action<bool> completed)
            {
                log.Add("rewarded");
                LastPurpose = purpose;
                PendingRewarded = completed;
            }
            public void ShowInterstitial(Action completed)
            {
                log.Add("interstitial");
                PendingInterstitial = completed;
            }
        }

        private sealed class FakeEvents : ILevelFlowEvents
        {
            private readonly Log log;
            public FakeEvents(Log log) => this.log = log;
            public void LevelStarted(int level) => log.Add("started" + level);
            public void LevelCompleted(int level, int score) => log.Add("completed" + level + ":" + score);
            public void LevelFailed(int level, string reason) => log.Add("failed" + level + ":" + reason);
        }

        private sealed class LoggingProgressStore : ILevelProgressStore
        {
            private readonly Log log;
            private int value;
            public LoggingProgressStore(Log log) => this.log = log;
            public int LoadHighestCompleted() => value;
            public void SaveHighestCompleted(int level)
            {
                value = level;
                log.Add("progress" + level);
            }
        }

        private sealed class LoggingBalanceStore : ICurrencyBalanceStore
        {
            private readonly Log log;
            private int balance;
            public LoggingBalanceStore(Log log) => this.log = log;
            public int Load() => balance;
            public void Save(int value)
            {
                balance = value;
                log.Add("coins" + value);
            }
        }

        private Log log;
        private FakeGameplay gameplay;
        private FakeView view;
        private FakeAds ads;
        private CurrencyWallet wallet;
        private LevelProgress progress;
        private LevelFlow flow;

        [SetUp]
        public void SetUp()
        {
            log = new Log();
            gameplay = new FakeGameplay(log);
            view = new FakeView(log);
            ads = new FakeAds(log);
            wallet = new CurrencyWallet(new LoggingBalanceStore(log));
            progress = new LevelProgress(new LoggingProgressStore(log));
            flow = new LevelFlow(Config());
        }

        private LevelFlowConfig Config() => new LevelFlowConfig
        {
            Gameplay = gameplay,
            View = view,
            Progress = progress,
            Wallet = wallet,
            RewardPolicy = outcome => outcome.Score * 10,
            Ads = ads,
            Events = new FakeEvents(log),
            LevelCount = 3,
            Commit = () => log.Add("commit"),
            ExitToHome = () => log.Add("home")
        };

        private void StartAndClear(int level = 1)
        {
            flow.Start(level);
            log.Clear();
        }

        [Test]
        public void Start_LogsThenBeginsLevel()
        {
            flow.Start(2);
            Assert.That(log.Text, Is.EqualTo("started2,begin2"));
            Assert.That(flow.CurrentLevel, Is.EqualTo(2));
        }

        [TestCase(0)]
        [TestCase(4)]
        public void Start_OutsideRange_Throws(int level)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => flow.Start(level));
        }

        [Test]
        public void Win_SavesBeforeShowingPopup()
        {
            StartAndClear();
            gameplay.Win(3);
            Assert.That(log.Text, Is.EqualTo("progress1,coins30,commit,completed1:3,showWin"));
            Assert.That(view.Win.Level, Is.EqualTo(1));
            Assert.That(view.Win.Score, Is.EqualTo(3));
            Assert.That(view.Win.Reward, Is.EqualTo(30));
        }

        [Test]
        public void Next_ShowsInterstitialThenStartsNextLevel()
        {
            StartAndClear();
            gameplay.Win();
            log.Clear();

            view.Win.Next();
            Assert.That(log.Text, Is.EqualTo("hide,interstitial"));
            ads.PendingInterstitial();
            Assert.That(log.Text, Is.EqualTo("hide,interstitial,started2,begin2"));
            Assert.That(wallet.Balance, Is.EqualTo(30), "Next must not add more coins.");
        }

        [Test]
        public void Next_WithoutAds_StartsNextLevel()
        {
            var config = Config();
            config.Ads = null;
            flow = new LevelFlow(config);
            StartAndClear();
            gameplay.Win();
            log.Clear();

            view.Win.Next();
            Assert.That(log.Text, Is.EqualTo("hide,started2,begin2"));
        }

        [Test]
        public void Next_OnLastLevel_ExitsToHome()
        {
            StartAndClear(3);
            gameplay.Win();
            view.Win.Next();
            log.Clear();
            ads.PendingInterstitial();
            Assert.That(log.Text, Is.EqualTo("home"));
        }

        [Test]
        public void Double_WhenEarned_AddsRewardOnceAndAdvancesWithoutInterstitial()
        {
            StartAndClear();
            gameplay.Win();
            log.Clear();

            bool? result = null;
            view.Win.Double(earned => result = earned);
            Assert.That(ads.LastPurpose, Is.EqualTo(LevelAdPurpose.DoubleReward));
            Assert.That(log.Text, Is.EqualTo("rewarded"));

            ads.PendingRewarded(true);
            Assert.That(result, Is.True);
            Assert.That(wallet.Balance, Is.EqualTo(60));
            Assert.That(log.Text, Is.EqualTo("rewarded,coins60,commit,hide,started2,begin2"));
        }

        [Test]
        public void Double_WhenNotEarned_KeepsOfferUsable()
        {
            StartAndClear();
            gameplay.Win();
            var offer = view.Win;

            bool? result = null;
            offer.Double(earned => result = earned);
            ads.PendingRewarded(false);
            Assert.That(result, Is.False);
            Assert.That(wallet.Balance, Is.EqualTo(30));

            log.Clear();
            offer.Next();
            Assert.That(log.Text, Is.EqualTo("hide,interstitial"));
        }

        [Test]
        public void Double_CallbackFiredTwice_AddsOnce()
        {
            StartAndClear();
            gameplay.Win();
            view.Win.Double();
            var callback = ads.PendingRewarded;
            callback(true);
            callback(true);
            Assert.That(wallet.Balance, Is.EqualTo(60));
        }

        [Test]
        public void Double_WhenAdNotReady_ReportsFalseWithoutShowing()
        {
            StartAndClear();
            gameplay.Win();
            ads.Ready = false;
            Assert.That(view.Win.CanDouble, Is.False);

            log.Clear();
            bool? result = null;
            view.Win.Double(earned => result = earned);
            Assert.That(result, Is.False);
            Assert.That(log, Is.Empty);
        }

        [Test]
        public void CanDouble_FollowsAdReadiness()
        {
            StartAndClear();
            gameplay.Win();
            Assert.That(view.Win.CanDouble, Is.True);
            ads.Ready = false;
            Assert.That(view.Win.CanDouble, Is.False);
        }

        [Test]
        public void Next_WhileRewardedShowing_IsIgnored()
        {
            StartAndClear();
            gameplay.Win();
            view.Win.Double();
            log.Clear();
            view.Win.Next();
            Assert.That(log, Is.Empty);
        }

        [Test]
        public void Next_CalledTwice_AdvancesOnce()
        {
            StartAndClear();
            gameplay.Win();
            var offer = view.Win;
            log.Clear();

            offer.Next();
            offer.Next();
            Assert.That(log.Text, Is.EqualTo("hide,interstitial"));

            var callback = ads.PendingInterstitial;
            callback();
            callback();
            Assert.That(flow.CurrentLevel, Is.EqualTo(2));
            Assert.That(log.Text, Is.EqualTo("hide,interstitial,started2,begin2"));
        }

        [Test]
        public void StaleWinOffer_CannotActAfterNextLevelStarted()
        {
            StartAndClear();
            gameplay.Win();
            var stale = view.Win;
            stale.Next();
            ads.PendingInterstitial();
            log.Clear();

            Assert.That(stale.CanDouble, Is.False);
            stale.Next();
            bool? result = null;
            stale.Double(earned => result = earned);
            Assert.That(log, Is.Empty);
            Assert.That(result, Is.False);
        }

        [Test]
        public void Finished_CalledTwice_CountsOnce()
        {
            StartAndClear();
            gameplay.Win();
            gameplay.Win();
            gameplay.Lose();
            Assert.That(wallet.Balance, Is.EqualTo(30));
            Assert.That(log.Text, Is.EqualTo("progress1,coins30,commit,completed1:3,showWin"));
        }

        [Test]
        public void Finished_BeforeStart_IsIgnored()
        {
            gameplay.Win();
            Assert.That(log, Is.Empty);
        }

        [Test]
        public void Lose_WithReadyAd_OffersRevive()
        {
            StartAndClear();
            gameplay.Lose();
            Assert.That(log.Text, Is.EqualTo("showRevive"));
        }

        [Test]
        public void Lose_WithoutReadyAd_GoesToLose()
        {
            StartAndClear();
            ads.Ready = false;
            gameplay.Lose("wall");
            Assert.That(log.Text, Is.EqualTo("failed1:wall,showLose"));
        }

        [Test]
        public void Lose_WithoutAds_GoesToLose()
        {
            var config = Config();
            config.Ads = null;
            flow = new LevelFlow(config);
            StartAndClear();
            gameplay.Lose("wall");
            Assert.That(log.Text, Is.EqualTo("failed1:wall,showLose"));
        }

        [Test]
        public void Watch_WhenEarned_RevivesAndKeepsPlaying()
        {
            StartAndClear();
            gameplay.Lose();
            log.Clear();

            bool? result = null;
            view.Revive.Watch(earned => result = earned);
            Assert.That(ads.LastPurpose, Is.EqualTo(LevelAdPurpose.Revive));
            ads.PendingRewarded(true);
            Assert.That(result, Is.True);
            Assert.That(log.Text, Is.EqualTo("rewarded,hide,revive"));

            log.Clear();
            gameplay.Win();
            Assert.That(log, Does.Contain("showWin"), "The level continues after reviving.");
        }

        [Test]
        public void Revive_IsOfferedOncePerAttempt()
        {
            StartAndClear();
            gameplay.Lose();
            view.Revive.Watch();
            ads.PendingRewarded(true);
            log.Clear();

            gameplay.Lose("second");
            Assert.That(log.Text, Is.EqualTo("failed1:second,showLose"));
        }

        [Test]
        public void Revive_FailedByGameplay_RestartsLevelWithFreshRevive()
        {
            gameplay.ReviveResult = false;
            StartAndClear();
            gameplay.Lose();
            view.Revive.Watch();
            log.Clear();
            ads.PendingRewarded(true);
            Assert.That(log.Text, Is.EqualTo("hide,revive,started1,begin1"));

            log.Clear();
            gameplay.Lose();
            Assert.That(log.Text, Is.EqualTo("showRevive"));
        }

        [Test]
        public void Watch_WhenNotEarned_KeepsOfferUsable()
        {
            StartAndClear();
            gameplay.Lose();
            var offer = view.Revive;
            bool? result = null;
            offer.Watch(earned => result = earned);
            log.Clear();
            ads.PendingRewarded(false);
            Assert.That(result, Is.False);
            Assert.That(log, Is.Empty);

            offer.Decline();
            Assert.That(log.Text, Is.EqualTo("hide,failed1:hit,showLose"));
        }

        [Test]
        public void Decline_WhileAdShowing_IsIgnored()
        {
            StartAndClear();
            gameplay.Lose();
            view.Revive.Watch();
            log.Clear();
            view.Revive.Decline();
            Assert.That(log, Is.Empty);
        }

        [Test]
        public void Retry_RestartsLevelWithFreshRevive()
        {
            StartAndClear();
            gameplay.Lose();
            view.Revive.Decline();
            log.Clear();

            view.Lose.Retry();
            Assert.That(log.Text, Is.EqualTo("hide,started1,begin1"));

            log.Clear();
            gameplay.Lose();
            Assert.That(log.Text, Is.EqualTo("showRevive"));
        }

        [Test]
        public void Home_FromLose_ExitsOnce()
        {
            StartAndClear();
            ads.Ready = false;
            gameplay.Lose();
            var offer = view.Lose;
            log.Clear();

            offer.Home();
            offer.Home();
            offer.Retry();
            Assert.That(log.Text, Is.EqualTo("hide,home"));
        }

        [Test]
        public void NoWallet_GivesNoRewardAndNoDouble()
        {
            var config = Config();
            config.Wallet = null;
            config.RewardPolicy = null;
            flow = new LevelFlow(config);
            StartAndClear();
            gameplay.Win();

            Assert.That(view.Win.Reward, Is.EqualTo(0));
            Assert.That(view.Win.CanDouble, Is.False);
            Assert.That(log.Text, Is.EqualTo("progress1,commit,completed1:3,showWin"));
        }

        [Test]
        public void ZeroReward_HasNoDouble()
        {
            StartAndClear();
            gameplay.Win(0);
            Assert.That(view.Win.Reward, Is.EqualTo(0));
            Assert.That(view.Win.CanDouble, Is.False);
        }

        [Test]
        public void NoEvents_StillRuns()
        {
            var config = Config();
            config.Events = null;
            flow = new LevelFlow(config);
            StartAndClear();
            ads.Ready = false;
            Assert.DoesNotThrow(() => gameplay.Lose());
            Assert.DoesNotThrow(() => view.Lose.Retry());
            Assert.DoesNotThrow(() => gameplay.Win());
        }

        [Test]
        public void GameException_IsNotSwallowed()
        {
            var config = Config();
            config.RewardPolicy = _ => throw new InvalidOperationException("reward bug");
            flow = new LevelFlow(config);
            StartAndClear();
            Assert.Throws<InvalidOperationException>(() => gameplay.Win());
        }

        [Test]
        public void Dispose_StopsListeningAndInvalidatesEverything()
        {
            StartAndClear();
            gameplay.Win();
            var offer = view.Win;
            offer.Double();
            var callback = ads.PendingRewarded;
            log.Clear();

            flow.Dispose();
            callback(true);
            offer.Next();
            gameplay.Lose();

            Assert.That(log, Is.Empty);
            Assert.That(wallet.Balance, Is.EqualTo(30));
            Assert.That(gameplay.Subscribers, Is.EqualTo(0));
            Assert.Throws<ObjectDisposedException>(() => flow.Start(1));
        }

        [Test]
        public void Dispose_DropsPendingInterstitialCallback()
        {
            StartAndClear();
            gameplay.Win();
            view.Win.Next();
            var callback = ads.PendingInterstitial;
            log.Clear();
            flow.Dispose();
            callback();
            Assert.That(log, Is.Empty);
        }

        [TestCase("Gameplay")]
        [TestCase("View")]
        [TestCase("Progress")]
        [TestCase("Commit")]
        [TestCase("ExitToHome")]
        [TestCase("LevelCount")]
        [TestCase("WalletWithoutPolicy")]
        [TestCase("PolicyWithoutWallet")]
        public void InvalidConfig_Throws(string problem)
        {
            var config = Config();
            switch (problem)
            {
                case "Gameplay": config.Gameplay = null; break;
                case "View": config.View = null; break;
                case "Progress": config.Progress = null; break;
                case "Commit": config.Commit = null; break;
                case "ExitToHome": config.ExitToHome = null; break;
                case "LevelCount": config.LevelCount = 0; break;
                case "WalletWithoutPolicy": config.RewardPolicy = null; break;
                case "PolicyWithoutWallet": config.Wallet = null; break;
            }

            Assert.Throws<ArgumentException>(() => new LevelFlow(config));
        }

        [Test]
        public void MissingConfig_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new LevelFlow(null));
        }
    }
}

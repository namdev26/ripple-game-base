using System;

namespace RippleGame.Flow
{
    public readonly struct LevelOutcome
    {
        public readonly bool Won;
        public readonly int Score;
        public readonly string FailReason;

        public LevelOutcome(bool won, int score, string failReason = null)
        {
            Won = won;
            Score = score;
            FailReason = failReason;
        }

        public static LevelOutcome Win(int score) => new LevelOutcome(true, score);
        public static LevelOutcome Lose(int score, string failReason = null) => new LevelOutcome(false, score, failReason);
    }

    /// <summary>The game's rules. The flow knows nothing else about how the game plays.</summary>
    public interface ILevelGameplay
    {
        /// <summary>Raise when the attempt ends. Repeated or late raises are ignored.</summary>
        event Action<LevelOutcome> Finished;
        void Begin(int level);
        /// <summary>Continue the current attempt. Return false if that is impossible, and the level restarts.</summary>
        bool TryRevive();
    }

    /// <summary>The game's screens. Popups call back through the offer they receive.</summary>
    public interface ILevelFlowView
    {
        void ShowWin(WinOffer offer);
        void ShowRevive(ReviveOffer offer);
        void ShowLose(LoseOffer offer);
        void HideAll();
    }

    public enum LevelAdPurpose
    {
        DoubleReward,
        Revive
    }

    public interface ILevelFlowAds
    {
        bool IsRewardedReady { get; }
        /// <param name="completed">Receives true only if the player earned the reward.</param>
        void ShowRewarded(LevelAdPurpose purpose, Action<bool> completed);
        /// <param name="completed">Must be called even when no ad could be shown.</param>
        void ShowInterstitial(Action completed);
    }

    public interface ILevelFlowEvents
    {
        void LevelStarted(int level);
        void LevelCompleted(int level, int score);
        void LevelFailed(int level, string reason);
    }
}

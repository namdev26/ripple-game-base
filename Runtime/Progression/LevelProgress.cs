using System;

namespace RippleGame.Progression
{
    /// <summary>Stores the highest completed level. Any save system can implement this.</summary>
    public interface ILevelProgressStore
    {
        int LoadHighestCompleted();
        void SaveHighestCompleted(int level);
    }

    /// <summary>Which levels are open. Level numbers start at 1 and progress never decreases.</summary>
    public sealed class LevelProgress
    {
        private readonly ILevelProgressStore store;

        public LevelProgress(ILevelProgressStore store)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public int HighestCompleted => Math.Max(0, store.LoadHighestCompleted());

        public bool IsUnlocked(int level) => level >= 1 && level <= HighestCompleted + 1;

        public void MarkCompleted(int level)
        {
            if (level < 1) throw new ArgumentOutOfRangeException(nameof(level), "Level numbers start at 1.");
            if (level > HighestCompleted) store.SaveHighestCompleted(level);
        }
    }
}

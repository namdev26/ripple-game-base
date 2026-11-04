using System;

namespace RippleGame.Flow
{
    /// <summary>What the win screen may do. Calls made after the offer has expired do nothing.</summary>
    public sealed class WinOffer
    {
        private readonly LevelFlow flow;
        private readonly int generation;

        public int Level { get; }
        public int Score { get; }
        /// <summary>Coins already added when the player won.</summary>
        public int Reward { get; }

        internal WinOffer(LevelFlow flow, int generation, int level, int score, int reward)
        {
            this.flow = flow;
            this.generation = generation;
            Level = level;
            Score = score;
            Reward = reward;
        }

        /// <summary>Reads ad state now, so a popup can check it again later.</summary>
        public bool CanDouble => flow.CanDouble(generation, Reward);
        public void Next() => flow.AcceptNext(generation);
        /// <param name="completed">Receives false if the reward was not earned. The offer then stays valid.</param>
        public void Double(Action<bool> completed = null) => flow.AcceptDouble(generation, Reward, completed);
    }

    /// <summary>What the revive screen may do. Calls made after the offer has expired do nothing.</summary>
    public sealed class ReviveOffer
    {
        private readonly LevelFlow flow;
        private readonly int generation;

        internal ReviveOffer(LevelFlow flow, int generation)
        {
            this.flow = flow;
            this.generation = generation;
        }

        /// <param name="completed">Receives false if the reward was not earned. The offer then stays valid.</param>
        public void Watch(Action<bool> completed = null) => flow.AcceptWatch(generation, completed);
        public void Decline() => flow.AcceptDecline(generation);
    }

    /// <summary>What the lose screen may do. Calls made after the offer has expired do nothing.</summary>
    public sealed class LoseOffer
    {
        private readonly LevelFlow flow;
        private readonly int generation;

        internal LoseOffer(LevelFlow flow, int generation)
        {
            this.flow = flow;
            this.generation = generation;
        }

        public void Retry() => flow.AcceptRetry(generation);
        public void Home() => flow.AcceptHome(generation);
    }
}

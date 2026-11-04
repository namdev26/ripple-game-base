using System;
using RippleGame.Flow;
using UnityEngine;

namespace RippleGame.Base.Samples
{
    /// <summary>Your game's rules. Replace the public methods with real gameplay.</summary>
    public sealed class ExampleGameplay : MonoBehaviour, ILevelGameplay
    {
        public event Action<LevelOutcome> Finished;

        public void Begin(int level) => Debug.Log("Playing level " + level);
        public bool TryRevive() => true;

        public void WinNow(int score) => Finished?.Invoke(LevelOutcome.Win(score));
        public void LoseNow() => Finished?.Invoke(LevelOutcome.Lose(0, "example"));
    }
}

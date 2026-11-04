using RippleGame.Flow;
using UnityEngine;

namespace RippleGame.Base.Samples
{
    /// <summary>Your game's screens. Show your popups here and connect their buttons to the offer.</summary>
    public sealed class ExampleView : MonoBehaviour, ILevelFlowView
    {
        public void ShowWin(WinOffer offer)
        {
            Debug.Log("Won level " + offer.Level + ", coins " + offer.Reward);
            offer.Next();
        }

        public void ShowRevive(ReviveOffer offer) => offer.Decline();
        public void ShowLose(LoseOffer offer) => offer.Retry();
        public void HideAll() { }
    }
}

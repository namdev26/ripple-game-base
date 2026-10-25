using RippleGame.UI;
using UnityEngine;

namespace RippleGame.Base.Tests
{
    public sealed class OpenPopupOnEnable : MonoBehaviour
    {
        [SerializeField] private PopupHost host;
        [SerializeField] private PopupView next;

        public void Configure(PopupHost popupHost, PopupView nextPrefab)
        {
            host = popupHost;
            next = nextPrefab;
        }

        private void OnEnable()
        {
            if (host == null) return;
            if (next != null) host.Show(next);
            else host.CloseActive();
        }
    }
}

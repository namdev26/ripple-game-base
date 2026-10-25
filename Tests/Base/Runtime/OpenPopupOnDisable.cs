using RippleGame.UI;
using UnityEngine;

namespace RippleGame.Base.Tests
{
    public sealed class OpenPopupOnDisable : MonoBehaviour
    {
        private PopupHost host;
        private PopupView next;

        public void Configure(PopupHost popupHost, PopupView nextPrefab)
        {
            host = popupHost;
            next = nextPrefab;
        }

        private void OnDisable()
        {
            var currentHost = host;
            host = null;
            if (currentHost != null) currentHost.Show(next);
        }
    }
}

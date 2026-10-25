using RippleGame.UI;
using UnityEngine;

namespace RippleGame.Base.Samples
{
    public sealed class PopupButtonExample : MonoBehaviour
    {
        [SerializeField] private PopupHost host;
        [SerializeField] private PopupView prefab;
        public void Open() => host.Show(prefab);
    }
}

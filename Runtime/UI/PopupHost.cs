using System;
using UnityEngine;

namespace RippleGame.UI
{
    /// <summary>Hosts one popup at a time; no resource paths, reward rules or game routes.</summary>
    public sealed class PopupHost : MonoBehaviour
    {
        [SerializeField] private Transform container;
        private PopupView active;

        public PopupView Active => active;
        public Transform Container
        {
            get => container != null ? container : transform;
            set => container = value;
        }

        public T Show<T>(T prefab) where T : PopupView
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            var instance = Instantiate(prefab, Container, false);
            CloseActive();
            active = instance;
            return instance;
        }

        public void CloseActive()
        {
            if (active != null) active.Close();
            active = null;
        }

        private void OnDestroy() => CloseActive();
    }
}

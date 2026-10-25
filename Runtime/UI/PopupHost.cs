using System;
using UnityEngine;

namespace RippleGame.UI
{
    /// <summary>Hosts one popup at a time; no resource paths, reward rules or game routes.</summary>
    public sealed class PopupHost : MonoBehaviour
    {
        [SerializeField] private Transform container;
        private PopupView active;
        private Transform staging;

        public PopupView Active => active;
        public Transform Container
        {
            get => container != null ? container : transform;
            set => container = value;
        }

        public T Show<T>(T prefab) where T : PopupView
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            var instance = Instantiate(prefab, GetStagingContainer(), false);
            instance.gameObject.SetActive(false);
            instance.transform.SetParent(Container, false);
            var previous = active;
            active = instance;
            if (previous != null) previous.Close();
            // A close callback may replace this request with a newer popup.
            if (instance != null && active == instance) instance.gameObject.SetActive(true);
            return instance;
        }

        public void CloseActive()
        {
            var previous = active;
            active = null;
            if (previous != null) previous.Close();
        }

        private Transform GetStagingContainer()
        {
            if (staging != null) return staging;
            var holder = new GameObject("Popup staging");
            holder.SetActive(false);
            staging = holder.transform;
            staging.SetParent(transform, false);
            return staging;
        }

        private void OnDestroy() => CloseActive();
    }
}

using System;
using UnityEngine;

namespace RippleGame.Ads.Max
{
    /// <summary>Hidden per-frame hook that drives manual banner/MREC refresh and the network reconnect check.</summary>
    [AddComponentMenu("")]
    internal sealed class MaxUpdateHook : MonoBehaviour
    {
        internal event Action Ticked;

        internal static MaxUpdateHook Create()
        {
            var gameObject = new GameObject("[RippleGame Ads MAX]") { hideFlags = HideFlags.HideInHierarchy };
            DontDestroyOnLoad(gameObject);
            return gameObject.AddComponent<MaxUpdateHook>();
        }

        internal void Release()
        {
            Ticked = null;
            if (this != null)
            {
                Destroy(gameObject);
            }
        }

        private void Update() => Ticked?.Invoke();
    }
}

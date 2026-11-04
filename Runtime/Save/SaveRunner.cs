using UnityEngine;

namespace RippleGame.Save
{
    /// <summary>Connects a save file to the app lifecycle: timed writes, and writes before the OS can kill the app.</summary>
    internal sealed class SaveRunner : MonoBehaviour
    {
        private SaveFile owner;

        internal static SaveRunner Create(SaveFile owner)
        {
            var holder = new GameObject("RippleGame Save") { hideFlags = HideFlags.HideInHierarchy };
            DontDestroyOnLoad(holder);
            var runner = holder.AddComponent<SaveRunner>();
            runner.owner = owner;
            return runner;
        }

        internal void Release()
        {
            owner = null;
            Destroy(gameObject);
        }

        private void Update() => owner?.Tick(Time.unscaledDeltaTime);

        private void OnApplicationPause(bool paused)
        {
            if (paused) owner?.Flush();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) owner?.Flush();
        }

        private void OnApplicationQuit() => owner?.Flush();
    }
}

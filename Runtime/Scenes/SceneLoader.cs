using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RippleGame.Scenes
{
    /// <summary>Loads scenes by a name the game supplies. Unknown names fail with a clear error.</summary>
    public static class SceneLoader
    {
        public static void Load(string sceneName)
        {
            Validate(sceneName);
            SceneManager.LoadScene(sceneName);
        }

        /// <summary>For a loading screen: run it with StartCoroutine.</summary>
        /// <param name="minSeconds">Shortest time the loading screen stays visible.</param>
        /// <param name="onProgress">Receives progress from 0 to 1.</param>
        public static IEnumerator LoadRoutine(string sceneName, float minSeconds = 0f, Action<float> onProgress = null)
        {
            Validate(sceneName);

            var operation = SceneManager.LoadSceneAsync(sceneName);
            operation.allowSceneActivation = false;

            var elapsed = 0f;
            while (true)
            {
                // Unity stops at 0.9 until activation is allowed.
                var loaded = Mathf.Clamp01(operation.progress / 0.9f);
                var waited = minSeconds > 0f ? Mathf.Clamp01(elapsed / minSeconds) : 1f;
                onProgress?.Invoke(Mathf.Min(loaded, waited));
                if (loaded >= 1f && waited >= 1f) break;

                yield return null;
                elapsed += Time.unscaledDeltaTime;
            }

            operation.allowSceneActivation = true;
            yield return operation;
        }

        private static void Validate(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
                throw new ArgumentException($"Scene '{sceneName}' is not in Build Settings.", nameof(sceneName));
        }
    }
}

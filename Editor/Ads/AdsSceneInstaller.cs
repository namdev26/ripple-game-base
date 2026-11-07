using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RippleGame.Ads.Editor
{
    internal static class AdsSceneInstaller
    {
        [MenuItem("RIPPLE Game/ADS/Scene Setup/Add Ads Object to Active Scene")]
        private static void AddAdsObjectToActiveScene()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[RippleGame Ads] Exit Play Mode before adding the scene object.");
                return;
            }

            var existing = Object.FindFirstObjectByType<AdsBootstrapper>();
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                EditorGUIUtility.PingObject(existing.gameObject);
                Debug.Log("[RippleGame Ads] An AdsBootstrapper already exists in a loaded scene.");
                return;
            }

            var gameObject = new GameObject("[RippleGame Ads]");
            Undo.RegisterCreatedObjectUndo(gameObject, "Create RippleGame Ads Object");
            gameObject.AddComponent<AdsBootstrapper>();
            gameObject.AddComponent<AdsSceneBannerRouter>();
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
            Selection.activeGameObject = gameObject;
            EditorGUIUtility.PingObject(gameObject);
            Debug.Log("[RippleGame Ads] Added to the active scene. Save the scene before testing.");
        }
    }
}

using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace RippleGame.Ads.Editor
{
    /// <summary>Updates the public GoogleMobileAdsSettings API without depending on a plugin-internal file path.</summary>
    internal static class AdMobSettingsSynchronizer
    {
        internal static void SynchronizeAndroidAppId(string appId)
        {
            var settingsAsset = FindGoogleMobileAdsSettingsAsset();
            if (settingsAsset == null)
            {
                throw new BuildFailedException(
                    "Google Mobile Ads settings asset was not found. Install the official plugin and open Assets > Google Mobile Ads > Settings once.");
            }

            var property = settingsAsset.GetType().GetProperty("GoogleMobileAdsAndroidAppId", BindingFlags.Instance | BindingFlags.Public);
            if (property == null || !property.CanWrite)
            {
                throw new BuildFailedException("The installed Google Mobile Ads plugin does not expose GoogleMobileAdsAndroidAppId. Update the plugin.");
            }

            property.SetValue(settingsAsset, appId);
            EditorUtility.SetDirty(settingsAsset);
            AssetDatabase.SaveAssets();
        }

        internal static bool IsGoogleMobileAdsInstalled()
        {
            return AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assembly.GetType("GoogleMobileAds.Api.MobileAds") != null);
        }

        private static ScriptableObject FindGoogleMobileAdsSettingsAsset()
        {
            foreach (var guid in AssetDatabase.FindAssets("GoogleMobileAdsSettings"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset != null && asset.GetType().FullName == "GoogleMobileAds.Editor.GoogleMobileAdsSettings")
                {
                    return asset;
                }
            }

            return null;
        }
    }
}

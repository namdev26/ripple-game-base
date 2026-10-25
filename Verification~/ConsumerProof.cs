using System.IO;
using System.Security.Cryptography;
using RippleGame.Ads;
using RippleGame.Analytics;
using RippleGame.Economy;
using RippleGame.Persistence;
using UnityEditor;
using UnityEngine;

internal static class ConsumerProof
{
    private const string AdsPath = "Assets/Settings/RippleGameAds/AdsConfiguration.asset";
    private const string AnalyticsPath = "Assets/Resources/RippleGameAnalytics/AnalyticsSettings.asset";
    private const string SaveKey = "ripple.base.verification.balance";

    internal static void Run()
    {
        var proofPath = Path.Combine(Directory.GetCurrentDirectory(), "ConsumerProof.json");
        if (!File.Exists(proofPath))
        {
            HubProof.Require(EditorApplication.ExecuteMenuItem("TOOL/ADS/Create Ads Configuration"), "create local ads configuration");
            HubProof.Require(EditorApplication.ExecuteMenuItem("TOOL/ANALYTICS/Create Analytics Settings"), "create local analytics configuration");
            var ads = AssetDatabase.LoadAssetAtPath<AdsConfiguration>(AdsPath);
            var analytics = AssetDatabase.LoadAssetAtPath<AnalyticsSettings>(AnalyticsPath);
            var serialized = new SerializedObject(ads);
            serialized.FindProperty("enableLogging").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            serialized = new SerializedObject(analytics);
            serialized.FindProperty("enableConsoleLog").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            var store = new PlayerPrefsBalanceStore(SaveKey);
            store.Save(37);
            File.WriteAllText(proofPath, JsonUtility.ToJson(new Snapshot
            {
                adsHash = Hash(AdsPath), analyticsHash = Hash(AnalyticsPath),
                adsGuid = AssetDatabase.AssetPathToGUID(AdsPath), analyticsGuid = AssetDatabase.AssetPathToGUID(AnalyticsPath)
            }, true));
        }
        else
        {
            var snapshot = JsonUtility.FromJson<Snapshot>(File.ReadAllText(proofPath));
            HubProof.Require(Hash(AdsPath) == snapshot.adsHash && Hash(AnalyticsPath) == snapshot.analyticsHash, "configuration content preserved across update");
            HubProof.Require(AssetDatabase.AssetPathToGUID(AdsPath) == snapshot.adsGuid && AssetDatabase.AssetPathToGUID(AnalyticsPath) == snapshot.analyticsGuid, "configuration GUIDs preserved");
            HubProof.Require(new CurrencyWallet(new PlayerPrefsBalanceStore(SaveKey)).Balance == 37, "saved balance preserved across update");
        }
        HubProof.Require(!AssetDatabase.LoadAssetAtPath<AdsConfiguration>(AdsPath).EnableLogging, "editable consumer ads asset");
        HubProof.Require(!AssetDatabase.LoadAssetAtPath<AnalyticsSettings>(AnalyticsPath).EnableConsoleLog, "editable consumer analytics asset");
    }

    private static string Hash(string path)
    {
        using (var algorithm = SHA256.Create()) return System.Convert.ToBase64String(algorithm.ComputeHash(File.ReadAllBytes(path)));
    }

    [System.Serializable]
    private sealed class Snapshot
    {
        public string adsHash;
        public string analyticsHash;
        public string adsGuid;
        public string analyticsGuid;
    }
}

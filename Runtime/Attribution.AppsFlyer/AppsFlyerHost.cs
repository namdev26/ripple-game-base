using System;
using System.Collections.Concurrent;
using AppsFlyerSDK;
using UnityEngine;

namespace RippleGame.Attribution.AppsFlyer
{
    /// <summary>
    /// Hidden object that receives the AppsFlyer conversion callbacks by name. Callbacks may arrive off the main thread,
    /// so they only queue work; <see cref="Update"/> runs it on the main thread.
    /// </summary>
    internal sealed class AppsFlyerHost : MonoBehaviour, IAppsFlyerConversionData
    {
        internal const string ObjectName = "RippleGame.AppsFlyer";

        private readonly ConcurrentQueue<Action> _mainThread = new ConcurrentQueue<Action>();
        private bool _debugLog;

        internal static AppsFlyerHost Create(bool debugLog)
        {
            var go = new GameObject(ObjectName)
            {
                hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector
            };
            DontDestroyOnLoad(go);

            var host = go.AddComponent<AppsFlyerHost>();
            host._debugLog = debugLog;
            return host;
        }

        private void Update()
        {
            while (_mainThread.TryDequeue(out var work))
            {
                try
                {
                    work();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        public void onConversionDataSuccess(string conversionData)
        {
            LogDebug("Conversion data: " + conversionData);

            (AttributionStatus status, string mediaSource, string campaign) result;
            try
            {
                result = ConversionDataParser.Parse(AppsFlyerSDK.AppsFlyer.CallbackStringToDictionary(conversionData));
            }
            catch (Exception exception)
            {
                _mainThread.Enqueue(() => Debug.LogWarning(Attribution.LogPrefix + "Could not read AppsFlyer conversion data: " + exception.Message));
                return;
            }

            _mainThread.Enqueue(() => Attribution.ReportConversion(result.status, result.mediaSource, result.campaign));
        }

        public void onConversionDataFail(string error) =>
            _mainThread.Enqueue(() => Debug.LogWarning(Attribution.LogPrefix + "AppsFlyer conversion data failed: " + error));

        public void onAppOpenAttribution(string attributionData) => LogDebug("App open attribution: " + attributionData);

        public void onAppOpenAttributionFailure(string error) => LogDebug("App open attribution failed: " + error);

        private void LogDebug(string message)
        {
            if (_debugLog) _mainThread.Enqueue(() => Debug.Log(Attribution.LogPrefix + message));
        }
    }
}

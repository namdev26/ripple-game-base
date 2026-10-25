#if FIREBASE && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using Firebase;
using Firebase.Extensions;
using UnityEngine;
using FirebaseSdk = Firebase.Analytics.FirebaseAnalytics;
using FirebaseParameter = Firebase.Analytics.Parameter;

namespace RippleGame.Analytics
{
    /// <summary>Firebase Analytics backend. This is the only file that knows the Firebase SDK API.</summary>
    internal sealed class FirebaseAnalyticsService : IAnalyticsService
    {
        private readonly bool _logToConsole;

        private FirebaseAnalyticsService(bool logToConsole) => _logToConsole = logToConsole;

        internal static void Initialize(AnalyticsSettings settings)
        {
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                var status = task.IsFaulted || task.IsCanceled ? DependencyStatus.UnavailableOther : task.Result;
                if (status != DependencyStatus.Available)
                {
                    Debug.LogError(Analytics.LogPrefix + "Firebase unavailable: " + (task.Exception?.GetBaseException().Message ?? status.ToString()));
                    Analytics.SetService(new DebugAnalyticsService(settings.EnableConsoleLog));
                    return;
                }

                try
                {
                    FirebaseSdk.SetAnalyticsCollectionEnabled(settings.CollectionEnabled);
                    Analytics.SetService(new FirebaseAnalyticsService(settings.EnableConsoleLog));
                }
                catch (Exception exception)
                {
                    Debug.LogError(Analytics.LogPrefix + "Firebase init failed: " + exception.Message);
                    Analytics.SetService(new DebugAnalyticsService(settings.EnableConsoleLog));
                }
            });
        }

        public void LogEvent(string name, IReadOnlyList<AnalyticsParam> parameters)
        {
            var converted = new FirebaseParameter[parameters.Count];
            for (var i = 0; i < parameters.Count; i++)
            {
                var parameter = parameters[i];
                switch (parameter.Kind)
                {
                    case AnalyticsParamKind.Long:
                        converted[i] = new FirebaseParameter(parameter.Name, parameter.LongValue);
                        break;
                    case AnalyticsParamKind.Double:
                        converted[i] = new FirebaseParameter(parameter.Name, parameter.DoubleValue);
                        break;
                    default:
                        converted[i] = new FirebaseParameter(parameter.Name, parameter.StringValue);
                        break;
                }
            }

            FirebaseSdk.LogEvent(name, converted);
            if (_logToConsole) Debug.Log(DebugAnalyticsService.Format(name, parameters));
        }

        public void SetUserProperty(string name, string value) => FirebaseSdk.SetUserProperty(name, value);
        public void SetUserId(string id) => FirebaseSdk.SetUserId(id);
        public void SetCollectionEnabled(bool enabled) => FirebaseSdk.SetAnalyticsCollectionEnabled(enabled);
    }
}
#endif

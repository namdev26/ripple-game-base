using System;
using System.Collections.Generic;
using UnityEngine;

namespace RippleGame.Analytics
{
    /// <summary>
    /// Single public entry point for analytics. Calls are validated against Firebase limits,
    /// queued until a service is ready, and never throw into game code.
    /// </summary>
    public static class Analytics
    {
        internal const int MaxQueuedCalls = 100;
        internal const string LogPrefix = "[RippleGame Analytics] ";

        private static readonly object Gate = new object();
        private static readonly Queue<Action<IAnalyticsService>> Pending = new Queue<Action<IAnalyticsService>>();
        private static IAnalyticsService _service;
        private static AnalyticsSettings _settings;
        private static bool _overflowWarned;

        public static AnalyticsSettings Settings
        {
            get
            {
                if (_settings == null) _settings = AnalyticsSettings.CreateDefault();
                return _settings;
            }
        }

        public static void LogEvent(string name, AnalyticsParams parameters = null)
        {
            var warnings = new List<string>();
            var valid = AnalyticsEventValidator.TrySanitizeEvent(name, parameters, out var sanitized, warnings);
            Warn(warnings);
            if (valid) Dispatch(service => service.LogEvent(name, sanitized));
        }

        public static void SetUserProperty(string name, string value)
        {
            var warnings = new List<string>();
            var valid = AnalyticsEventValidator.TrySanitizeUserProperty(name, value, out var sanitized, warnings);
            Warn(warnings);
            if (valid) Dispatch(service => service.SetUserProperty(name, sanitized));
        }

        public static void SetUserId(string id) => Dispatch(service => service.SetUserId(id));

        public static void SetCollectionEnabled(bool enabled) => Dispatch(service => service.SetCollectionEnabled(enabled));

        internal static void SetSettings(AnalyticsSettings settings) => _settings = settings;

        internal static void SetService(IAnalyticsService service)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));

            Action<IAnalyticsService>[] flush;
            lock (Gate)
            {
                _service = service;
                _overflowWarned = false;
                flush = Pending.ToArray();
                Pending.Clear();
            }

            foreach (var call in flush) Invoke(service, call);
        }

        internal static void ResetForTests()
        {
            lock (Gate)
            {
                Pending.Clear();
                _service = null;
                _settings = null;
                _overflowWarned = false;
            }
        }

        private static void Dispatch(Action<IAnalyticsService> call)
        {
            IAnalyticsService service;
            var overflowed = false;
            lock (Gate)
            {
                service = _service;
                if (service == null)
                {
                    if (Pending.Count == MaxQueuedCalls)
                    {
                        Pending.Dequeue();
                        overflowed = !_overflowWarned;
                        _overflowWarned = true;
                    }

                    Pending.Enqueue(call);
                }
            }

            if (overflowed) Warn(new List<string> { $"More than {MaxQueuedCalls} calls before analytics was ready; oldest calls are being dropped." });
            if (service != null) Invoke(service, call);
        }

        private static void Invoke(IAnalyticsService service, Action<IAnalyticsService> call)
        {
            try
            {
                call(service);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(LogPrefix + "Service call failed: " + exception.Message);
            }
        }

        private static void Warn(List<string> warnings)
        {
            if (warnings.Count == 0 || !Debug.isDebugBuild) return;
            foreach (var warning in warnings) Debug.LogWarning(LogPrefix + warning);
        }
    }
}

using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.Extensions;
using UnityEngine;

namespace RippleGame.Firebase
{
    /// <summary>
    /// Initializes Firebase once for every RippleGame Firebase feature. This assembly only compiles with the
    /// pinned com.google.firebase.app package.
    /// </summary>
    public static class FirebaseBootstrap
    {
        private const string LogPrefix = "[RippleGame Firebase] ";

        private static readonly object Gate = new object();
        private static Task<bool> _ready;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            lock (Gate) _ready = null;
        }

        /// <summary>
        /// Resolves true once Firebase dependencies are available. The check runs once and every caller shares the
        /// same task. Any failure resolves false and logs the reason; the task never faults.
        /// </summary>
        public static Task<bool> EnsureReadyAsync()
        {
            lock (Gate)
            {
                if (_ready == null) _ready = Start();
                return _ready;
            }
        }

        private static Task<bool> Start()
        {
            var source = new TaskCompletionSource<bool>();
            try
            {
                FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
                {
                    try
                    {
                        if (task.IsFaulted || task.IsCanceled)
                            Fail(source, task.Exception?.GetBaseException().Message ?? "dependency check was canceled");
                        else if (task.Result != DependencyStatus.Available)
                            Fail(source, task.Result.ToString());
                        else
                            source.TrySetResult(true);
                    }
                    catch (Exception exception)
                    {
                        Fail(source, exception.Message);
                    }
                });
            }
            catch (Exception exception)
            {
                Fail(source, exception.Message);
            }

            return source.Task;
        }

        private static void Fail(TaskCompletionSource<bool> source, string reason)
        {
            Debug.LogError(LogPrefix + "Firebase unavailable: " + reason);
            source.TrySetResult(false);
        }
    }
}

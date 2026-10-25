using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;

namespace RippleGame.Ads
{
    internal sealed class MainThreadDispatcher : MonoBehaviour
    {
        private static readonly ConcurrentQueue<Action> Pending = new ConcurrentQueue<Action>();
        private static MainThreadDispatcher _instance;
        private static int _mainThreadId;

        internal static void EnsureInitialized()
        {
            if (_instance != null)
            {
                return;
            }

            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
            var gameObject = new GameObject("[RippleGame Ads Dispatcher]");
            DontDestroyOnLoad(gameObject);
            _instance = gameObject.AddComponent<MainThreadDispatcher>();
        }

        internal static void Post(Action action)
        {
            if (action == null)
            {
                return;
            }

            if (Thread.CurrentThread.ManagedThreadId == _mainThreadId)
            {
                action();
                return;
            }

            Pending.Enqueue(action);
        }

        private void Update()
        {
            while (Pending.TryDequeue(out var action))
            {
                try
                {
                    action();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }
    }
}

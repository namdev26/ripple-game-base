using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RippleGame.RemoteConfig.Tests
{
    public sealed class RemoteConfigTests
    {
        private sealed class SamplePolicy
        {
            public int Cooldown = 30;
            public bool Enabled;
            public string Name = "default";
        }

        private FakeRemoteConfigSource _source;
        private int _activated;
        private readonly List<string> _warnings = new List<string>();
        private NetworkReachability _reachability;
        private Func<NetworkReachability> _savedReachability;

        [SetUp]
        public void SetUp()
        {
            RemoteConfig.ResetForTests();
            _source = new FakeRemoteConfigSource();
            _activated = 0;
            _warnings.Clear();
            RemoteConfig.Activated += OnActivated;
            Application.logMessageReceived += OnLog;
            _savedReachability = RemoteConfigNetworkWatcher.ReachabilityProvider;
            _reachability = NetworkReachability.ReachableViaCarrierDataNetwork;
            RemoteConfigNetworkWatcher.ReachabilityProvider = () => _reachability;
        }

        [TearDown]
        public void TearDown()
        {
            Application.logMessageReceived -= OnLog;
            RemoteConfig.ResetForTests();
            RemoteConfigNetworkWatcher.ReachabilityProvider = _savedReachability;
        }

        private void OnActivated() => _activated++;

        private void OnLog(string message, string stackTrace, LogType type)
        {
            if (type == LogType.Warning && message.StartsWith("[RippleGame RemoteConfig]")) _warnings.Add(message);
        }

        private static IEnumerator WaitUntil(Func<bool> condition, string what, float seconds = 2f)
        {
            var end = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < end) yield return null;
            Assert.That(condition(), Is.True, "timed out waiting for: " + what);
        }

        // ---- Lookup order and typed getters ----

        [Test]
        public void Get_SourceValueWins_ThenSettingsDefault_ThenArgument()
        {
            RemoteConfig.SetSettings(RemoteConfigSettings.CreateForTests(5f, 0,
                new RemoteConfigDefault("from_source", "2"),
                new RemoteConfigDefault("from_settings", "3")));
            _source.Values["from_source"] = "10";
            RemoteConfig.RegisterSource(_source);

            Assert.That(RemoteConfig.Get("from_source", 1), Is.EqualTo(10));
            Assert.That(RemoteConfig.Get("from_settings", 1), Is.EqualTo(3));
            Assert.That(RemoteConfig.Get("missing", 1), Is.EqualTo(1));
        }

        [Test]
        public void Get_SupportedTypes()
        {
            _source.Values["i"] = "7";
            _source.Values["l"] = "8000000000";
            _source.Values["f"] = "1.5";
            _source.Values["d"] = "2.5";
            _source.Values["b"] = "true";
            _source.Values["s"] = "hello";
            RemoteConfig.RegisterSource(_source);

            Assert.That(RemoteConfig.Get("i", 0), Is.EqualTo(7));
            Assert.That(RemoteConfig.Get("l", 0L), Is.EqualTo(8000000000L));
            Assert.That(RemoteConfig.Get("f", 0f), Is.EqualTo(1.5f));
            Assert.That(RemoteConfig.Get("d", 0d), Is.EqualTo(2.5d));
            Assert.That(RemoteConfig.Get("b", false), Is.True);
            Assert.That(RemoteConfig.Get("s", ""), Is.EqualTo("hello"));
        }

        [Test]
        public void Get_WithoutSource_UsesSettingsDefaultThenArgument()
        {
            RemoteConfig.SetSettings(RemoteConfigSettings.CreateForTests(5f, 0, new RemoteConfigDefault("ads_enabled", "false")));

            Assert.That(RemoteConfig.Get("ads_enabled", true), Is.False);
            Assert.That(RemoteConfig.Get("other", true), Is.True);
        }

        [Test]
        public void Get_InvalidInt_ReturnsDefault_WarnsOncePerKey()
        {
            _source.Values["level"] = "abc";
            _source.Values["other"] = "xyz";
            RemoteConfig.RegisterSource(_source);

            Assert.That(RemoteConfig.Get("level", 4), Is.EqualTo(4));
            Assert.That(RemoteConfig.Get("level", 4), Is.EqualTo(4));
            Assert.That(RemoteConfig.Get("level", 4), Is.EqualTo(4));
            Assert.That(_warnings, Is.EqualTo(new[] { "[RippleGame RemoteConfig] Key 'level' is not a valid int. Using the default." }));

            Assert.That(RemoteConfig.Get("other", 9), Is.EqualTo(9));
            Assert.That(_warnings, Has.Count.EqualTo(2));
        }

        [Test]
        public void Get_InvalidSourceValue_FallsBackToSettingsDefault()
        {
            RemoteConfig.SetSettings(RemoteConfigSettings.CreateForTests(5f, 0, new RemoteConfigDefault("level", "6")));
            _source.Values["level"] = "abc";
            RemoteConfig.RegisterSource(_source);

            Assert.That(RemoteConfig.Get("level", 4), Is.EqualTo(6));
            Assert.That(_warnings, Has.Count.EqualTo(1));
        }

        // ---- JSON ----

        [Test]
        public void GetJson_PartialFields_KeepConstructorDefaults_AndIgnoreUnknownFields()
        {
            _source.Values["policy"] = "{\"Enabled\":true,\"Unknown\":{\"x\":1}}";
            RemoteConfig.RegisterSource(_source);

            var policy = RemoteConfig.GetJson("policy", new SamplePolicy { Cooldown = 99 });

            Assert.That(policy.Enabled, Is.True);
            Assert.That(policy.Cooldown, Is.EqualTo(30), "an absent field keeps the type's own default");
            Assert.That(policy.Name, Is.EqualTo("default"));
        }

        [Test]
        public void GetJson_Invalid_ReturnsArgument_WarnsOncePerKey()
        {
            _source.Values["policy"] = "{not json";
            RemoteConfig.RegisterSource(_source);
            var fallback = new SamplePolicy { Cooldown = 99 };

            Assert.That(RemoteConfig.GetJson("policy", fallback), Is.SameAs(fallback));
            Assert.That(RemoteConfig.GetJson("policy", fallback), Is.SameAs(fallback));
            Assert.That(_warnings, Has.Count.EqualTo(1));
            StringAssert.Contains("Key 'policy' is not a valid SamplePolicy", _warnings[0]);
        }

        [Test]
        public void GetJson_MissingKeyOrJsonNull_ReturnsArgument()
        {
            _source.Values["nothing"] = "null";
            RemoteConfig.RegisterSource(_source);
            var fallback = new SamplePolicy();

            Assert.That(RemoteConfig.GetJson("missing", fallback), Is.SameAs(fallback));
            Assert.That(RemoteConfig.GetJson("nothing", fallback), Is.SameAs(fallback));
        }

        [Test]
        public void GetJson_UsesSettingsDefaultWhenSourceHasNoValue()
        {
            RemoteConfig.SetSettings(RemoteConfigSettings.CreateForTests(5f, 0, new RemoteConfigDefault("policy", "{\"Cooldown\":12}")));
            RemoteConfig.RegisterSource(_source);

            Assert.That(RemoteConfig.GetJson("policy", new SamplePolicy()).Cooldown, Is.EqualTo(12));
        }

        // ---- Readiness ----

        [Test]
        public void NoSource_IsReadyImmediately()
        {
            RemoteConfig.SetSettings(RemoteConfigSettings.CreateForTests(5f, 0));
            Assert.That(RemoteConfig.IsReady, Is.False, "not ready before the bootstrap ran");

            var task = RemoteConfigBootstrap.Run(RemoteConfig.Settings);

            Assert.That(task.IsCompleted, Is.True);
            Assert.That(RemoteConfig.IsReady, Is.True);
            Assert.That(_activated, Is.EqualTo(0));
            Assert.That(RemoteConfigNetworkWatcher.Exists, Is.False);
            Assert.That(RemoteConfig.WaitUntilReadyAsync(5f).IsCompleted, Is.True);
        }

        [UnityTest]
        public IEnumerator FetchSuccess_RaisesActivatedOnce()
        {
            var settings = RemoteConfigSettings.CreateForTests(2f, 60, new RemoteConfigDefault("a", "1"));
            _source.Delay = TimeSpan.FromMilliseconds(50);
            RemoteConfig.RegisterSource(_source);
            RemoteConfig.SetSettings(settings);

            var run = RemoteConfigBootstrap.Run(settings);
            Assert.That(RemoteConfig.IsReady, Is.False);
            yield return WaitUntil(() => RemoteConfig.IsReady, "ready");
            yield return WaitUntil(() => run.IsCompleted, "bootstrap task");

            Assert.That(_activated, Is.EqualTo(1));
            Assert.That(_source.FetchCount, Is.EqualTo(1));
            Assert.That(_source.LastTimeout, Is.EqualTo(TimeSpan.FromSeconds(2)));
            Assert.That(_source.LastMinimumInterval, Is.EqualTo(TimeSpan.FromSeconds(settings.MinimumFetchIntervalSeconds)));
            Assert.That(_source.Defaults, Contains.Key("a"));
            Assert.That(RemoteConfigNetworkWatcher.Exists, Is.False, "a successful first fetch needs no network watcher");
        }

        [UnityTest]
        public IEnumerator FetchTimeout_StillBecomesReady_NoActivatedEvent()
        {
            var settings = RemoteConfigSettings.CreateForTests(0.1f, 0);
            _source.Delay = TimeSpan.FromMilliseconds(500);
            _source.Result = true; // would succeed, but only after the timeout
            RemoteConfig.RegisterSource(_source);
            RemoteConfig.SetSettings(settings);

            var run = RemoteConfigBootstrap.Run(settings);
            yield return WaitUntil(() => RemoteConfig.IsReady, "ready after timeout", 1f);
            yield return WaitUntil(() => run.IsCompleted, "bootstrap task", 1f);
            Assert.That(_activated, Is.EqualTo(0), "Activated is not raised at the timeout itself");
            Assert.That(RemoteConfig.IsReady, Is.True);
            Assert.That(RemoteConfigNetworkWatcher.Exists, Is.True, "the first fetch did not succeed, so the watcher waits for the network");
        }

        [UnityTest]
        public IEnumerator LateSuccessAfterTimeout_RaisesActivatedOnce_NewValuesLive_WatcherRemoved()
        {
            var settings = RemoteConfigSettings.CreateForTests(0.1f, 0);
            _source.Delay = TimeSpan.FromMilliseconds(400);
            _source.Result = true;
            _source.Values["k"] = "old";
            _source.ValuesAfterFetch["k"] = "new";
            RemoteConfig.RegisterSource(_source);

            var run = RemoteConfigBootstrap.Run(settings);
            yield return WaitUntil(() => RemoteConfig.IsReady, "ready after timeout", 1f);
            yield return WaitUntil(() => run.IsCompleted, "bootstrap task", 1f);
            Assert.That(_activated, Is.EqualTo(0));
            Assert.That(RemoteConfig.Get("k", ""), Is.EqualTo("old"));
            Assert.That(RemoteConfigNetworkWatcher.Exists, Is.True);

            yield return WaitUntil(() => _activated == 1, "late activation", 1.5f);
            Assert.That(RemoteConfig.IsReady, Is.True);
            Assert.That(RemoteConfig.Get("k", ""), Is.EqualTo("new"));
            Assert.That(RemoteConfigNetworkWatcher.Exists, Is.False);

            for (var i = 0; i < 5; i++) yield return null;
            Assert.That(_activated, Is.EqualTo(1), "exactly once");
        }

        [UnityTest]
        public IEnumerator LateFailureAfterTimeout_DoesNotActivate_AndKeepsWatcher()
        {
            var settings = RemoteConfigSettings.CreateForTests(0.1f, 0);
            _source.Delay = TimeSpan.FromMilliseconds(300);
            _source.Result = false;
            RemoteConfig.RegisterSource(_source);

            var run = RemoteConfigBootstrap.Run(settings);
            yield return WaitUntil(() => run.IsCompleted, "bootstrap task", 1f);
            var end = Time.realtimeSinceStartup + 0.5f;
            while (Time.realtimeSinceStartup < end) yield return null;

            Assert.That(_source.FetchCount, Is.EqualTo(1));
            Assert.That(RemoteConfig.IsReady, Is.True);
            Assert.That(_activated, Is.EqualTo(0));
            Assert.That(RemoteConfigNetworkWatcher.Exists, Is.True);
        }

        [UnityTest]
        public IEnumerator LateThrowAfterTimeout_DoesNotActivate_AndKeepsWatcher()
        {
            var settings = RemoteConfigSettings.CreateForTests(0.1f, 0);
            _source.Delay = TimeSpan.FromMilliseconds(300);
            _source.ThrowOnFetch = true;
            RemoteConfig.RegisterSource(_source);

            var run = RemoteConfigBootstrap.Run(settings);
            yield return WaitUntil(() => run.IsCompleted, "bootstrap task", 1f);
            var end = Time.realtimeSinceStartup + 0.5f;
            while (Time.realtimeSinceStartup < end) yield return null;

            Assert.That(_activated, Is.EqualTo(0));
            Assert.That(RemoteConfigNetworkWatcher.Exists, Is.True);
        }

        [UnityTest]
        public IEnumerator RetryThatTimesOutButSucceedsLate_RaisesActivated_AndRemovesWatcher()
        {
            var settings = RemoteConfigSettings.CreateForTests(0.1f, 0);
            _source.Results.Enqueue(false);
            _source.Results.Enqueue(true);
            _source.ValuesAfterFetch["k"] = "new";
            _reachability = NetworkReachability.NotReachable;
            RemoteConfig.RegisterSource(_source);

            var run = RemoteConfigBootstrap.Run(settings);
            yield return WaitUntil(() => run.IsCompleted, "first fetch", 1f);
            Assert.That(RemoteConfigNetworkWatcher.Exists, Is.True);

            _source.Delay = TimeSpan.FromMilliseconds(300);
            _reachability = NetworkReachability.ReachableViaLocalAreaNetwork;
            RemoteConfigNetworkWatcher.Instance.Poll();
            yield return WaitUntil(() => _source.FetchCount == 2, "retry started", 1f);
            Assert.That(_activated, Is.EqualTo(0));

            yield return WaitUntil(() => _activated == 1, "late retry activation", 1.5f);
            Assert.That(RemoteConfig.Get("k", ""), Is.EqualTo("new"));
            Assert.That(RemoteConfigNetworkWatcher.Exists, Is.False);
        }

        [UnityTest]
        public IEnumerator LateSuccessAfterReset_IsIgnored()
        {
            var settings = RemoteConfigSettings.CreateForTests(0.1f, 0);
            _source.Delay = TimeSpan.FromMilliseconds(300);
            RemoteConfig.RegisterSource(_source);

            var run = RemoteConfigBootstrap.Run(settings);
            yield return WaitUntil(() => run.IsCompleted, "bootstrap task", 1f);
            RemoteConfig.ResetForTests();
            RemoteConfig.Activated += OnActivated;
            var end = Time.realtimeSinceStartup + 0.5f;
            while (Time.realtimeSinceStartup < end) yield return null;

            Assert.That(_activated, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator FetchFailure_StillBecomesReady_NoActivatedEvent()
        {
            var settings = RemoteConfigSettings.CreateForTests(1f, 0);
            _source.Result = false;
            RemoteConfig.RegisterSource(_source);

            var run = RemoteConfigBootstrap.Run(settings);
            yield return WaitUntil(() => run.IsCompleted, "bootstrap task");

            Assert.That(RemoteConfig.IsReady, Is.True);
            Assert.That(_activated, Is.EqualTo(0));
            Assert.That(RemoteConfigNetworkWatcher.Exists, Is.True);
        }

        [UnityTest]
        public IEnumerator FetchThrows_StillBecomesReady_NoActivatedEvent()
        {
            var settings = RemoteConfigSettings.CreateForTests(1f, 0);
            _source.ThrowOnFetch = true;
            RemoteConfig.RegisterSource(_source);

            var run = RemoteConfigBootstrap.Run(settings);
            yield return WaitUntil(() => run.IsCompleted, "bootstrap task");

            Assert.That(run.IsFaulted, Is.False, "the bootstrap task never faults");
            Assert.That(RemoteConfig.IsReady, Is.True);
            Assert.That(_activated, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator WaitUntilReady_ReturnsAfterTimeoutWhenNeverReady()
        {
            var settings = RemoteConfigSettings.CreateForTests(3f, 0);
            _source.Delay = TimeSpan.FromSeconds(2);
            RemoteConfig.RegisterSource(_source);
            RemoteConfigBootstrap.Run(settings);

            var wait = RemoteConfig.WaitUntilReadyAsync(0.15f);
            yield return WaitUntil(() => wait.IsCompleted, "wait to give up", 1f);

            Assert.That(RemoteConfig.IsReady, Is.False);
            Assert.That(wait.IsFaulted, Is.False);
        }

        [UnityTest]
        public IEnumerator WaitUntilReady_CompletesAsSoonAsReady()
        {
            var settings = RemoteConfigSettings.CreateForTests(3f, 0);
            _source.Delay = TimeSpan.FromMilliseconds(100);
            RemoteConfig.RegisterSource(_source);
            RemoteConfigBootstrap.Run(settings);

            var wait = RemoteConfig.WaitUntilReadyAsync(10f);
            yield return WaitUntil(() => wait.IsCompleted, "wait to complete", 1.5f);

            Assert.That(RemoteConfig.IsReady, Is.True);
        }

        [UnityTest]
        public IEnumerator ResetForTests_IgnoresAFetchThatFinishesAfterwards()
        {
            var settings = RemoteConfigSettings.CreateForTests(2f, 0);
            _source.Delay = TimeSpan.FromMilliseconds(200);
            RemoteConfig.RegisterSource(_source);
            var run = RemoteConfigBootstrap.Run(settings);

            RemoteConfig.ResetForTests();
            RemoteConfig.Activated += OnActivated;
            yield return WaitUntil(() => run.IsCompleted, "stale bootstrap task");

            Assert.That(RemoteConfig.IsReady, Is.False);
            Assert.That(_activated, Is.EqualTo(0));
        }

        [Test]
        public void ActivatedHandlerThatThrows_DoesNotBreakOtherHandlers()
        {
            RemoteConfig.Activated -= OnActivated;
            RemoteConfig.Activated += () => throw new InvalidOperationException("boom");
            RemoteConfig.Activated += OnActivated;
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Activated handler failed"));

            RemoteConfig.RaiseActivated();

            Assert.That(_activated, Is.EqualTo(1));
        }

        [Test]
        public void Settings_WhenAssetAbsent_UseCodeDefaults()
        {
            var settings = RemoteConfigSettings.LoadOrDefault();

            Assert.That(settings.FetchTimeoutSeconds, Is.EqualTo(5f));
#if ADS_TEST
            Assert.That(settings.MinimumFetchIntervalSeconds, Is.EqualTo(0));
#else
            Assert.That(settings.MinimumFetchIntervalSeconds, Is.EqualTo(3600));
#endif
            Assert.That(settings.Defaults, Is.Empty);
        }

        // ---- Reconnect retry ----

        [UnityTest]
        public IEnumerator NetworkReturns_RetriesFetch_ThenActivatesAndRemovesWatcher()
        {
            var settings = RemoteConfigSettings.CreateForTests(1f, 0);
            _source.Results.Enqueue(false);
            _source.Results.Enqueue(true);
            _reachability = NetworkReachability.NotReachable;
            RemoteConfig.RegisterSource(_source);

            var run = RemoteConfigBootstrap.Run(settings);
            yield return WaitUntil(() => run.IsCompleted, "first fetch");
            Assert.That(RemoteConfig.IsReady, Is.True);
            Assert.That(RemoteConfigNetworkWatcher.Exists, Is.True);

            RemoteConfigNetworkWatcher.Instance.Poll();
            Assert.That(_source.FetchCount, Is.EqualTo(1), "still offline, no retry");

            _reachability = NetworkReachability.ReachableViaLocalAreaNetwork;
            RemoteConfigNetworkWatcher.Instance.Poll();
            yield return WaitUntil(() => _activated == 1, "retry activation");

            Assert.That(_source.FetchCount, Is.EqualTo(2));
            Assert.That(RemoteConfigNetworkWatcher.Exists, Is.False);
        }

        [UnityTest]
        public IEnumerator FailedRetry_KeepsWatcher_AndRetriesOnNextReconnect()
        {
            var settings = RemoteConfigSettings.CreateForTests(1f, 0);
            _source.Result = false;
            _reachability = NetworkReachability.NotReachable;
            RemoteConfig.RegisterSource(_source);

            var run = RemoteConfigBootstrap.Run(settings);
            yield return WaitUntil(() => run.IsCompleted, "first fetch");

            _reachability = NetworkReachability.ReachableViaCarrierDataNetwork;
            RemoteConfigNetworkWatcher.Instance.Poll();
            yield return WaitUntil(() => _source.FetchCount == 2, "first retry");
            yield return null;
            Assert.That(RemoteConfigNetworkWatcher.Exists, Is.True);
            Assert.That(_activated, Is.EqualTo(0));

            _reachability = NetworkReachability.NotReachable;
            RemoteConfigNetworkWatcher.Instance.Poll();
            _source.Result = true;
            _reachability = NetworkReachability.ReachableViaCarrierDataNetwork;
            RemoteConfigNetworkWatcher.Instance.Poll();
            yield return WaitUntil(() => _activated == 1, "second retry activation");

            Assert.That(_source.FetchCount, Is.EqualTo(3));
            Assert.That(RemoteConfigNetworkWatcher.Exists, Is.False);
        }

        [Test]
        public void Watcher_FiresOncePerOfflineToOnlineTransition()
        {
            var fired = 0;
            _reachability = NetworkReachability.ReachableViaLocalAreaNetwork;
            var watcher = RemoteConfigNetworkWatcher.Create(() => fired++);

            watcher.Poll();
            watcher.Poll();
            Assert.That(fired, Is.EqualTo(0), "online all along");

            _reachability = NetworkReachability.NotReachable;
            watcher.Poll();
            Assert.That(fired, Is.EqualTo(0));

            _reachability = NetworkReachability.ReachableViaCarrierDataNetwork;
            watcher.Poll();
            watcher.Poll();
            Assert.That(fired, Is.EqualTo(1));

            _reachability = NetworkReachability.NotReachable;
            watcher.Poll();
            _reachability = NetworkReachability.ReachableViaLocalAreaNetwork;
            watcher.Poll();
            Assert.That(fired, Is.EqualTo(2));
        }

        [Test]
        public void Watcher_IsHiddenAndSurvivesSceneLoads()
        {
            var watcher = RemoteConfigNetworkWatcher.Create(() => { });

            Assert.That(watcher.gameObject.hideFlags, Is.EqualTo(HideFlags.HideAndDontSave));
            Assert.That(RemoteConfigNetworkWatcher.Create(() => { }), Is.SameAs(watcher), "one watcher at a time");
            RemoteConfigNetworkWatcher.DestroyInstance();
            Assert.That(RemoteConfigNetworkWatcher.Exists, Is.False);
        }
    }
}

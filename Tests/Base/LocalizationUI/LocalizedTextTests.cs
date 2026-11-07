using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using RippleGame.Localization;
using RippleGame.Localization.UI;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace RippleGame.Base.Tests
{
    public sealed class LocalizedTextTests
    {
        private sealed class FakeSource : ILocalizationSource
        {
            private readonly Dictionary<string, string> files = new Dictionary<string, string>
            {
                ["English"] = "KEY_lang,English\ns_home,HOME",
                ["Vietnamese"] = "KEY_lang,Vietnamese\ns_home,TRANG CHỦ"
            };

            public string ReadOrNull(string language) => files.TryGetValue(language, out var content) ? content : null;
        }

        private sealed class FakePreference : ILanguagePreference
        {
            public string Load() => null;
            public void Save(string language) { }
        }

        private GameObject owner;
        private TMP_Text label;

        [SetUp]
        public void SetUp()
        {
            Localizer.Reset();
            owner = new GameObject("Label");
            owner.SetActive(false);
            label = owner.AddComponent<TextMeshProUGUI>();
            label.text = "Placeholder";
            owner.AddComponent<LocalizedText>().Key = "s_home";
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(owner);
            Localizer.Reset();
        }

        private static void Init()
        {
            Localizer.Init(new[] { "English", "Vietnamese" }, new LocalizationOptions
            {
                Source = new FakeSource(),
                Preference = new FakePreference(),
                DeviceLanguage = "English"
            });
        }

        [Test]
        public void UpdatesOnEnable()
        {
            Init();

            owner.SetActive(true);

            Assert.That(label.text, Is.EqualTo("HOME"));
        }

        [Test]
        public void UpdatesOnLanguageChange()
        {
            Init();
            owner.SetActive(true);

            Localizer.SetLanguage("Vietnamese");

            Assert.That(label.text, Is.EqualTo("TRANG CHỦ"));
        }

        [Test]
        public void InactiveText_CorrectAfterEnable()
        {
            Init();
            owner.SetActive(true);
            owner.SetActive(false);

            Localizer.SetLanguage("Vietnamese");
            owner.SetActive(true);

            Assert.That(label.text, Is.EqualTo("TRANG CHỦ"));
        }

        [Test]
        public void InitAfterEnable_Updates()
        {
            owner.SetActive(true);
            Assert.That(label.text, Is.EqualTo("Placeholder"));

            Init();

            Assert.That(label.text, Is.EqualTo("HOME"));
        }

        [Test]
        public void InitAgain_StillUpdates()
        {
            Init();
            owner.SetActive(true);
            Init();

            Localizer.SetLanguage("Vietnamese");

            Assert.That(label.text, Is.EqualTo("TRANG CHỦ"));
        }

        [UnityTest]
        public IEnumerator DestroyedText_NoError()
        {
            Init();
            owner.SetActive(true);
            Object.Destroy(owner);
            yield return null;

            Assert.DoesNotThrow(() => Localizer.SetLanguage("Vietnamese"));
        }

        [Test]
        public void EmptyKey_KeepsText()
        {
            Init();
            owner.GetComponent<LocalizedText>().Key = string.Empty;

            owner.SetActive(true);

            Assert.That(label.text, Is.EqualTo("Placeholder"));
        }
    }
}

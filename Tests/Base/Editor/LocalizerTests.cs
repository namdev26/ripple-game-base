using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using RippleGame.Localization;
using UnityEngine;
using UnityEngine.TestTools;

namespace RippleGame.Base.Tests
{
    public sealed class LocalizerTests
    {
        private sealed class FakeSource : ILocalizationSource
        {
            internal readonly Dictionary<string, string> Files = new Dictionary<string, string>
            {
                ["English"] = "KEY_lang,English\ns_home,HOME\ns_coin,Coin: {0}\ns_bad,Coin: {1}",
                ["Vietnamese"] = "KEY_lang,Vietnamese\ns_home,TRANG CHỦ\ns_coin,Xu: {0}",
                ["Chinese"] = "KEY_lang,Chinese\ns_home,主页"
            };

            internal int Reads;

            public string ReadOrNull(string language)
            {
                Reads++;
                return Files.TryGetValue(language, out var content) ? content : null;
            }
        }

        private sealed class FakePreference : ILanguagePreference
        {
            internal string Stored;
            internal string Saved;

            public string Load() => Stored;
            public void Save(string language) => Saved = language;
        }

        private static readonly string[] EnglishVietnamese = { "English", "Vietnamese" };

        private FakeSource source;
        private FakePreference preference;

        [SetUp]
        public void SetUp()
        {
            Localizer.Reset();
            source = new FakeSource();
            preference = new FakePreference();
        }

        [TearDown]
        public void TearDown() => Localizer.Reset();

        private void Init(string[] languages = null, string device = "English")
        {
            Localizer.Init(languages ?? EnglishVietnamese, new LocalizationOptions
            {
                Source = source,
                Preference = preference,
                DeviceLanguage = device
            });
        }

        [Test]
        public void Init_WithoutEnglish_Throws()
        {
            Assert.Throws<ArgumentException>(() => Init(new[] { "Vietnamese" }));
            Assert.That(Localizer.IsInitialized, Is.False);
        }

        [Test]
        public void Init_MissingEnglishFile_Throws()
        {
            source.Files.Remove("English");

            Assert.Throws<InvalidOperationException>(() => Init());
            Assert.That(Localizer.IsInitialized, Is.False);
        }

        [Test]
        public void Get_BeforeInit_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => Localizer.Get("s_home"));
        }

        [Test]
        public void Format_BeforeInit_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => Localizer.Format("s_coin", 1));
        }

        [Test]
        public void Init_UsesSavedLanguage()
        {
            preference.Stored = "Vietnamese";

            Init();

            Assert.That(Localizer.Language, Is.EqualTo("Vietnamese"));
            Assert.That(Localizer.Get("s_home"), Is.EqualTo("TRANG CHỦ"));
        }

        [Test]
        public void Init_SavedNotInList_UsesDevice()
        {
            preference.Stored = "German";

            Init(device: "Vietnamese");

            Assert.That(Localizer.Language, Is.EqualTo("Vietnamese"));
        }

        [Test]
        public void Init_DeviceNotInList_UsesEnglish()
        {
            Init(device: "Korean");

            Assert.That(Localizer.Language, Is.EqualTo("English"));
        }

        [TestCase("ChineseSimplified")]
        [TestCase("ChineseTraditional")]
        public void Init_ChineseVariant_MapsToChinese(string device)
        {
            Init(new[] { "English", "Chinese" }, device);

            Assert.That(Localizer.Language, Is.EqualTo("Chinese"));
        }

        [Test]
        public void Init_DoesNotSavePreference()
        {
            Init(device: "Vietnamese");

            Assert.That(preference.Saved, Is.Null);
        }

        [Test]
        public void Init_RaisesChanged()
        {
            var raised = 0;
            Localizer.Changed += () => raised++;

            Init();

            Assert.That(raised, Is.EqualTo(1));
        }

        [Test]
        public void Init_Again_KeepsSubscribers()
        {
            var raised = 0;
            Localizer.Changed += () => raised++;

            Init();
            Init();

            Assert.That(raised, Is.EqualTo(2));
        }

        [Test]
        public void Init_SavedLanguageFileMissing_FallsBack()
        {
            preference.Stored = "Vietnamese";
            source.Files.Remove("Vietnamese");
            LogAssert.Expect(LogType.Warning, new Regex("Vietnamese"));

            Init();

            Assert.That(Localizer.Language, Is.EqualTo("English"));
            Assert.That(Localizer.Get("s_home"), Is.EqualTo("HOME"));
        }

        [Test]
        public void Languages_ReturnsDeclaredList()
        {
            Init();

            Assert.That(Localizer.Languages, Is.EqualTo(EnglishVietnamese));
        }

        [Test]
        public void Get_Found()
        {
            Init();

            Assert.That(Localizer.Get("s_home"), Is.EqualTo("HOME"));
        }

        [Test]
        public void Get_MissingKey_ReturnsKey()
        {
            Init();
            LogAssert.Expect(LogType.Warning, new Regex("s_nothing"));

            Assert.That(Localizer.Get("s_nothing"), Is.EqualTo("s_nothing"));
        }

        [Test]
        public void SetLanguage_SwitchesAndSaves()
        {
            Init();
            var raised = 0;
            Localizer.Changed += () => raised++;

            Localizer.SetLanguage("Vietnamese");

            Assert.That(Localizer.Language, Is.EqualTo("Vietnamese"));
            Assert.That(Localizer.Get("s_home"), Is.EqualTo("TRANG CHỦ"));
            Assert.That(preference.Saved, Is.EqualTo("Vietnamese"));
            Assert.That(raised, Is.EqualTo(1));
        }

        [Test]
        public void SetLanguage_Same_DoesNothing()
        {
            Init();
            var raised = 0;
            var reads = source.Reads;
            Localizer.Changed += () => raised++;

            Localizer.SetLanguage("English");

            Assert.That(raised, Is.EqualTo(0));
            Assert.That(source.Reads, Is.EqualTo(reads));
            Assert.That(preference.Saved, Is.Null);
        }

        [Test]
        public void SetLanguage_NotInList_Throws()
        {
            Init();

            Assert.Throws<ArgumentException>(() => Localizer.SetLanguage("German"));
            Assert.That(Localizer.Language, Is.EqualTo("English"));
        }

        [Test]
        public void SetLanguage_MissingFile_FallsBack()
        {
            Init(new[] { "English", "Vietnamese", "Japanese" });
            Localizer.SetLanguage("Vietnamese");
            preference.Saved = null;
            LogAssert.Expect(LogType.Warning, new Regex("Japanese"));

            Localizer.SetLanguage("Japanese");

            Assert.That(Localizer.Language, Is.EqualTo("English"));
            Assert.That(Localizer.Get("s_home"), Is.EqualTo("HOME"));
            Assert.That(preference.Saved, Is.Null);
        }

        [Test]
        public void Format_InsertsArgs()
        {
            Init();

            Assert.That(Localizer.Format("s_coin", 5), Is.EqualTo("Coin: 5"));
        }

        [Test]
        public void Format_BadTemplate_ReturnsTemplate()
        {
            Init();
            LogAssert.Expect(LogType.Error, new Regex("s_bad"));

            Assert.That(Localizer.Format("s_bad", 5), Is.EqualTo("Coin: {1}"));
        }
    }
}

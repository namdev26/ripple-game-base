using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using RippleGame.Localization;

namespace RippleGame.Base.Tests
{
    public sealed class LocalizationSheetTests
    {
        private sealed class OneLanguageSource : ILocalizationSource
        {
            private readonly string content;
            internal OneLanguageSource(string content) => this.content = content;
            public string ReadOrNull(string language) => content;
        }

        private sealed class NoPreference : ILanguagePreference
        {
            public string Load() => null;
            public void Save(string language) { }
        }

        private string directory;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "RippleSheetTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
        }

        [TearDown]
        public void TearDown()
        {
            Localizer.Reset();
            Directory.Delete(directory, true);
        }

        private static void AssertFailed(LocalizationSheetResult result, string errorContains)
        {
            Assert.That(result.Ok, Is.False);
            Assert.That(result.Files, Is.Empty);
            Assert.That(string.Join("\n", result.Errors), Does.Contain(errorContains));
        }

        [Test]
        public void SheetCsv_QuotesCommasNewlines()
        {
            Assert.That(SheetCsv.Parse("a,\"b, c\",\"d\ne\"")[0], Is.EqualTo(new[] { "a", "b, c", "d\ne" }));
        }

        [Test]
        public void SheetCsv_EscapedQuote()
        {
            Assert.That(SheetCsv.Parse("\"a\"\"b\"")[0], Is.EqualTo(new[] { "a\"b" }));
        }

        [Test]
        public void SheetCsv_Crlf()
        {
            var rows = SheetCsv.Parse("a,b\r\nc,d");

            Assert.That(rows.Count, Is.EqualTo(2));
            Assert.That(rows[0], Is.EqualTo(new[] { "a", "b" }));
            Assert.That(rows[1], Is.EqualTo(new[] { "c", "d" }));
        }

        [Test]
        public void SheetUrl_EditLink()
        {
            Assert.That(
                SheetUrl.ToCsvExport("https://docs.google.com/spreadsheets/d/ABC/edit?gid=7#gid=7"),
                Is.EqualTo("https://docs.google.com/spreadsheets/d/ABC/export?format=csv&gid=7"));
        }

        [Test]
        public void SheetUrl_ExportLinkUnchanged()
        {
            const string url = "https://docs.google.com/spreadsheets/d/ABC/export?format=csv&gid=3";

            Assert.That(SheetUrl.ToCsvExport(" " + url + " "), Is.EqualTo(url));
        }

        [Test]
        public void SheetUrl_NoGid_UsesZero()
        {
            Assert.That(
                SheetUrl.ToCsvExport("https://docs.google.com/spreadsheets/d/ABC/edit"),
                Is.EqualTo("https://docs.google.com/spreadsheets/d/ABC/export?format=csv&gid=0"));
        }

        [Test]
        public void SheetUrl_Invalid_Throws()
        {
            Assert.Throws<ArgumentException>(() => SheetUrl.ToCsvExport("https://example.com/nothing"));
        }

        [Test]
        public void Build_Html_Fails()
        {
            AssertFailed(LocalizationSheet.Build("\n<!DOCTYPE html><html>Sign in</html>"), "share");
        }

        [Test]
        public void Build_WrongFirstColumn_Fails()
        {
            AssertFailed(LocalizationSheet.Build("Name,English\nk,v"), "KEY_lang");
        }

        [Test]
        public void Build_MissingEnglish_Fails()
        {
            AssertFailed(LocalizationSheet.Build("KEY_lang,Vietnamese\nk,v"), "English");
        }

        [Test]
        public void Build_BadColumnName_Fails()
        {
            AssertFailed(LocalizationSheet.Build("KEY_lang,English,../Evil\nk,v,x"), "../Evil");
        }

        [Test]
        public void Build_DuplicateColumn_Fails()
        {
            AssertFailed(LocalizationSheet.Build("KEY_lang,English,English\nk,v,x"), "English");
        }

        [Test]
        public void Build_DuplicateKey_Fails()
        {
            AssertFailed(LocalizationSheet.Build("KEY_lang,English\ns_a,1\ns_a,2"), "s_a");
        }

        [Test]
        public void Build_KeyWithComma_Fails()
        {
            AssertFailed(LocalizationSheet.Build("KEY_lang,English\n\"s,a\",1"), "s,a");
        }

        [Test]
        public void Build_PlaceholderMismatch_Fails()
        {
            var result = LocalizationSheet.Build("KEY_lang,English,Vietnamese\ns_a,a {0},b");

            AssertFailed(result, "s_a");
            AssertFailed(result, "Vietnamese");
        }

        [Test]
        public void Build_EmptyCell_UsesEnglish()
        {
            var result = LocalizationSheet.Build("KEY_lang,English,Vietnamese\ns_a,Hello,");

            Assert.That(result.Ok, Is.True);
            Assert.That(result.Files["Vietnamese"], Is.EqualTo("KEY_lang,Vietnamese\ns_a,Hello\n"));
        }

        [Test]
        public void Build_KeepsArabicAndUnknown()
        {
            var result = LocalizationSheet.Build("KEY_lang,English,Arabic,Unknown\ns_a,Hello,x,y");

            Assert.That(result.Ok, Is.True);
            Assert.That(result.Files.Keys, Is.EquivalentTo(new[] { "English", "Arabic", "Unknown" }));
            Assert.That(result.Files["Arabic"], Is.EqualTo("KEY_lang,Arabic\ns_a,x\n"));
        }

        [Test]
        public void Build_SkipsKeyLangRow()
        {
            var result = LocalizationSheet.Build("KEY_lang,English\ncountry_code,en\nKEY_lang,English\ns_a,Hello");

            Assert.That(result.Ok, Is.True);
            Assert.That(result.Files["English"], Is.EqualTo("KEY_lang,English\ncountry_code,en\ns_a,Hello\n"));
        }

        [Test]
        public void Build_CellNewline_WrittenEscaped()
        {
            var result = LocalizationSheet.Build("KEY_lang,English\ns_a,\"a\nb\"");

            Assert.That(result.Files["English"], Is.EqualTo("KEY_lang,English\ns_a,a\\nb\n"));
        }

        [Test]
        public void Build_BlankRowsAndColumns()
        {
            var result = LocalizationSheet.Build("KEY_lang,English,,\ns_a,Hello,,\n,,,\n\ns_b,World,,");

            Assert.That(result.Ok, Is.True);
            Assert.That(result.Files.Keys, Is.EquivalentTo(new[] { "English" }));
            Assert.That(result.Files["English"], Is.EqualTo("KEY_lang,English\ns_a,Hello\ns_b,World\n"));
        }

        [Test]
        public void Build_TrimsHeader()
        {
            var result = LocalizationSheet.Build("KEY_lang, English , Vietnamese \ns_a,Hello,Chao");

            Assert.That(result.Ok, Is.True);
            Assert.That(result.Files.Keys, Is.EquivalentTo(new[] { "English", "Vietnamese" }));
        }

        [Test]
        public void Build_TemplateWithBlankHeaderCells()
        {
            var result = LocalizationSheet.Build(
                ",,Vietnamese,Spanish,Unknown\n" +
                "country_code,en,vi,es,hi\n" +
                "KEY_lang,English,Vietnamese,Español,INDIA\n" +
                "s_home,HOME,TRANG CHỦ,HOGAR,x");

            Assert.That(result.Ok, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.Files.Keys, Is.EquivalentTo(new[] { "English", "Vietnamese", "Spanish", "Unknown" }));
            Assert.That(result.Files["Vietnamese"], Is.EqualTo("KEY_lang,Vietnamese\ncountry_code,vi\ns_home,TRANG CHỦ\n"));
        }

        [Test]
        public void Build_BlankFirstRow_UsesNextRowAsHeader()
        {
            var result = LocalizationSheet.Build(",,,\nKEY_lang,English,Vietnamese\ns_a,Hi,Chao");

            Assert.That(result.Ok, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.Files["Vietnamese"], Is.EqualTo("KEY_lang,Vietnamese\ns_a,Chao\n"));
        }

        [Test]
        public void Build_BlankFirstRow_KeepsColumnOrder()
        {
            var result = LocalizationSheet.Build(",,,\nKEY_lang,Vietnamese,English\ns_a,Chao,Hi");

            Assert.That(result.Files["English"], Is.EqualTo("KEY_lang,English\ns_a,Hi\n"));
            Assert.That(result.Files["Vietnamese"], Is.EqualTo("KEY_lang,Vietnamese\ns_a,Chao\n"));
        }

        [Test]
        public void Build_BlankSheet_Fails()
        {
            AssertFailed(LocalizationSheet.Build(",,,\n,,,\n"), "KEY_lang");
        }

        [Test]
        public void Build_NoKeys_Fails()
        {
            AssertFailed(LocalizationSheet.Build("KEY_lang,English,Vietnamese\n,,\n"), "no keys");
        }

        [Test]
        public void Build_RoundTrip()
        {
            const string original = "say \"hi\", a\nb";
            var csv = "KEY_lang,English\ns_a,\"say \"\"hi\"\", a\nb\"";

            var file = LocalizationSheet.Build(csv).Files["English"];
            Localizer.Init(new[] { "English" }, new LocalizationOptions
            {
                Source = new OneLanguageSource(file),
                Preference = new NoPreference(),
                DeviceLanguage = "English"
            });

            Assert.That(Localizer.Get("s_a"), Is.EqualTo(original));
        }

        [Test]
        public void Write_EnglishFileName()
        {
            var report = LocalizationFileWriter.Write(directory, Files(("English", "e"), ("Vietnamese", "v")));

            Assert.That(File.ReadAllText(Path.Combine(directory, "Localization.txt")), Is.EqualTo("e"));
            Assert.That(File.ReadAllText(Path.Combine(directory, "Localization-Vietnamese.txt")), Is.EqualTo("v"));
            Assert.That(report.Written, Is.EquivalentTo(new[] { "Localization.txt", "Localization-Vietnamese.txt" }));
        }

        [Test]
        public void Write_Utf8NoBom()
        {
            LocalizationFileWriter.Write(directory, Files(("Vietnamese", "Chủ")));

            var bytes = File.ReadAllBytes(Path.Combine(directory, "Localization-Vietnamese.txt"));
            Assert.That(bytes.SequenceEqual(new UTF8Encoding(false).GetBytes("Chủ")), Is.True);
        }

        [Test]
        public void Write_Unchanged_NotRewritten()
        {
            var path = Path.Combine(directory, "Localization.txt");
            File.WriteAllText(path, "same");
            var old = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            File.SetLastWriteTimeUtc(path, old);

            var report = LocalizationFileWriter.Write(directory, Files(("English", "same")));

            Assert.That(report.Unchanged, Is.EqualTo(new[] { "Localization.txt" }));
            Assert.That(report.Written, Is.Empty);
            Assert.That(File.GetLastWriteTimeUtc(path), Is.EqualTo(old));
        }

        [Test]
        public void Write_ReportsRemovedKeys()
        {
            File.WriteAllText(Path.Combine(directory, "Localization.txt"), "KEY_lang,English\nold,1\nkept,2\n");

            var report = LocalizationFileWriter.Write(directory, Files(("English", "KEY_lang,English\nkept,2\n")));

            Assert.That(report.RemovedKeys["English"], Is.EqualTo(new[] { "old" }));
        }

        [Test]
        public void Write_CreatesFolder()
        {
            var nested = Path.Combine(directory, "Assets", "Resources");

            LocalizationFileWriter.Write(nested, Files(("English", "e")));

            Assert.That(File.Exists(Path.Combine(nested, "Localization.txt")), Is.True);
        }

        private static Dictionary<string, string> Files(params (string language, string content)[] files)
        {
            return files.ToDictionary(file => file.language, file => file.content);
        }
    }
}

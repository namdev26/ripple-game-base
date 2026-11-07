using System.Text.RegularExpressions;
using NUnit.Framework;
using RippleGame.Localization;
using UnityEngine;
using UnityEngine.TestTools;

namespace RippleGame.Base.Tests
{
    public sealed class LocalizationFileTests
    {
        [Test]
        public void Parse_SkipsHeaderLine()
        {
            var entries = LocalizationFile.Parse("KEY_lang,English\nk,v");

            Assert.That(entries.Count, Is.EqualTo(1));
            Assert.That(entries["k"], Is.EqualTo("v"));
        }

        [Test]
        public void Parse_QuotedComma()
        {
            Assert.That(LocalizationFile.Parse("h,e\nk,\"a, b\"")["k"], Is.EqualTo("a, b"));
        }

        [Test]
        public void Parse_EscapedQuote()
        {
            Assert.That(LocalizationFile.Parse("h,e\nk,\"say \"\"hi\"\"\"")["k"], Is.EqualTo("say \"hi\""));
        }

        [Test]
        public void Parse_LiteralNewline()
        {
            Assert.That(LocalizationFile.Parse("h,e\nk,a\\nb")["k"], Is.EqualTo("a\nb"));
        }

        [Test]
        public void Parse_UnquotedKeepsCommas()
        {
            Assert.That(LocalizationFile.Parse("h,e\nk,a,b")["k"], Is.EqualTo("a,b"));
        }

        [Test]
        public void Parse_BomAndCrlf()
        {
            var entries = LocalizationFile.Parse("﻿h,e\r\nk,v\r\n");

            Assert.That(entries.Count, Is.EqualTo(1));
            Assert.That(entries["k"], Is.EqualTo("v"));
        }

        [Test]
        public void Parse_BlankLinesIgnored()
        {
            Assert.That(LocalizationFile.Parse("h,e\n\nk,v\n").Count, Is.EqualTo(1));
        }

        [Test]
        public void Parse_DuplicateKeepsLast()
        {
            LogAssert.Expect(LogType.Warning, new Regex("k"));

            Assert.That(LocalizationFile.Parse("h,e\nk,1\nk,2")["k"], Is.EqualTo("2"));
        }
    }
}

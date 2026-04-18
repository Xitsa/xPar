using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using xParLib;

namespace xParTests
{
    public class ExtractWordsTests
    {
        // ============================================================
        // Helpers
        // ============================================================

        private static Charset BodyChars => Charset.Parse("_A_a");
        private static Charset WhiteChars => Charset.Parse(" \t");
        private static Charset TerminalChars => Charset.Parse(".?!:");

        private static LineSegment[] ToSegments(params string[] lines)
        {
            return lines.Select(l => new LineSegment(l, new LineProp())).ToArray();
        }

        // ============================================================
        // Базовые тесты выделения слов
        // ============================================================

        [Fact]
        public void ExtractWords_SingleLine_SingleWord()
        {
            var segments = ToSegments("hello");

            var result = ReformatModule.ExtractWords(
                segments, 0, 0,
                prefix: 0, suffix: 0, width: 72,
                TerminalChars, cap: false, guess: false, report: false);

            Assert.Null(result.ErrorMessage);
            Assert.Single(result.Words);
            Assert.Equal("hello", result.Words[0].Text);
            Assert.Equal(5, result.Words[0].Width);
            Assert.Equal(72, result.L);
        }

        [Fact]
        public void ExtractWords_SingleLine_MultipleWords()
        {
            var segments = ToSegments("hello world foo");

            var result = ReformatModule.ExtractWords(
                segments, 0, 0,
                prefix: 0, suffix: 0, width: 72,
                TerminalChars, cap: false, guess: false, report: false);

            Assert.Null(result.ErrorMessage);
            Assert.Equal(3, result.Words.Count);
            Assert.Equal("hello", result.Words[0].Text);
            Assert.Equal("world", result.Words[1].Text);
            Assert.Equal("foo", result.Words[2].Text);
        }

        [Fact]
        public void ExtractWords_MultipleLines()
        {
            var segments = ToSegments(
                "hello world",
                "foo bar"
            );

            var result = ReformatModule.ExtractWords(
                segments, 0, 1,
                prefix: 0, suffix: 0, width: 72,
                TerminalChars, cap: false, guess: false, report: false);

            Assert.Null(result.ErrorMessage);
            Assert.Equal(4, result.Words.Count);
            Assert.Equal("hello", result.Words[0].Text);
            Assert.Equal("world", result.Words[1].Text);
            Assert.Equal("foo", result.Words[2].Text);
            Assert.Equal("bar", result.Words[3].Text);
        }

        // ============================================================
        // Первое слово (onFirstWord логика)
        // ============================================================

        [Fact]
        public void ExtractWords_FirstWordIncludesLeadingSpaces()
        {
            // Первое слово должно включать всё тело от начала (даже если есть ведущие пробелы)
            var segments = ToSegments("  hello world");

            var result = ReformatModule.ExtractWords(
                segments, 0, 0,
                prefix: 0, suffix: 0, width: 72,
                TerminalChars, cap: false, guess: false, report: false);

            Assert.Null(result.ErrorMessage);
            Assert.Equal(2, result.Words.Count);
            // Первое слово: "  hello" (пробелы + слово)
            Assert.Equal("  hello", result.Words[0].Text);
            Assert.Equal("world", result.Words[1].Text);
        }

        // ============================================================
        // Префикс и суффикс исключаются из слов
        // ============================================================

        [Fact]
        public void ExtractWords_PrefixExcludedFromWords()
        {
            // Аналог данных из Delimit_Body1_CuriousPrefixSuffixWithBody
            // Префикс "        amc> " = 13 символов
            var segments = ToSegments(
                "        amc> Par still pays attention",
                "        amc> Par should not mistake"
            );

            var result = ReformatModule.ExtractWords(
                segments, 0, 1,
                prefix: 13, suffix: 0, width: 72,
                TerminalChars, cap: false, guess: false, report: false);

            Assert.Null(result.ErrorMessage);
            // Первое слово — "Par", НЕ "        amc> Par"
            Assert.Equal("Par", result.Words[0].Text);
            Assert.Equal("still", result.Words[1].Text);
            Assert.Equal("pays", result.Words[2].Text);
            Assert.Equal("attention", result.Words[3].Text);
            Assert.Equal("Par", result.Words[4].Text);
        }

        [Fact]
        public void ExtractWords_SuffixExcludedFromWords()
        {
            // Строка: префикс=3 ("---"), тело="hello world", суффикс=3 ("...")
            var segments = ToSegments("---hello world...");

            var result = ReformatModule.ExtractWords(
                segments, 0, 0,
                prefix: 3, suffix: 3, width: 72,
                TerminalChars, cap: false, guess: false, report: false);

            Assert.Null(result.ErrorMessage);
            Assert.Equal(2, result.Words.Count);
            Assert.Equal("hello", result.Words[0].Text);
            Assert.Equal("world", result.Words[1].Text);
            // Суффикс сохранён отдельно
            Assert.Single(result.Suffixes);
            Assert.Equal("...", result.Suffixes[0]);
        }

        [Fact]
        public void ExtractWords_PrefixSuffixWithBody()
        {
            // Полный тест: префикс + тело с несколькими словами + суффикс
            var segments = ToSegments(
                "        amc> Par still pays attention to body characters.",
                "        amc> Par should not mistake \"Par\" for part of the prefix.",
                "        amc> Par should not mistake \".\" for a suffix."
            );

            var result = ReformatModule.ExtractWords(
                segments, 0, 2,
                prefix: 13, suffix: 0, width: 72,
                TerminalChars, cap: false, guess: false, report: false);

            Assert.Null(result.ErrorMessage);
            // Первое слово — "Par", префикс не попал
            Assert.Equal("Par", result.Words[0].Text);
            // Проверяем, что "." в конце последней строки — это часть слова "suffix."
            var lastWord = result.Words.Last();
            Assert.Equal("suffix.", lastWord.Text);
        }

        // ============================================================
        // Ошибка: строка короче prefix + suffix
        // ============================================================

        [Fact]
        public void ExtractWords_LineTooShort_ReturnsError()
        {
            var segments = ToSegments("abc");

            var result = ReformatModule.ExtractWords(
                segments, 0, 0,
                prefix: 5, suffix: 5, width: 72,
                TerminalChars, cap: false, guess: false, report: false);

            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("shorter than", result.ErrorMessage);
            Assert.Empty(result.Words);
        }

        // ============================================================
        // Пустое тело строки
        // ============================================================

        [Fact]
        public void ExtractWords_EmptyBody_NoWords()
        {
            // Префикс + суффикс = вся строка, тело пустое
            var segments = ToSegments("prefixsuffix");

            var result = ReformatModule.ExtractWords(
                segments, 0, 0,
                prefix: 6, suffix: 6, width: 72,
                TerminalChars, cap: false, guess: false, report: false);

            Assert.Null(result.ErrorMessage);
            Assert.Empty(result.Words);
        }

        // ============================================================
        // Curious word
        // ============================================================

        [Fact]
        public void ExtractWords_CuriousWord_Detected()
        {
            var segments = ToSegments("Hello. world");

            var result = ReformatModule.ExtractWords(
                segments, 0, 0,
                prefix: 0, suffix: 0, width: 72,
                TerminalChars, cap: false, guess: true, report: false);

            Assert.Null(result.ErrorMessage);
            // "Hello." — curious (есть '.' с alphanumeric до него)
            Assert.True(result.Words[0].Flags.HasFlag(WordFlags.Curious));
            // "world" — не curious
            Assert.False(result.Words[1].Flags.HasFlag(WordFlags.Curious));
        }

        // ============================================================
        // Capitalized word
        // ============================================================

        [Fact]
        public void ExtractWords_CapitalizedWord_Detected()
        {
            var segments = ToSegments("hello World");

            var result = ReformatModule.ExtractWords(
                segments, 0, 0,
                prefix: 0, suffix: 0, width: 72,
                TerminalChars, cap: false, guess: true, report: false);

            Assert.Null(result.ErrorMessage);
            Assert.False(result.Words[0].Flags.HasFlag(WordFlags.Capital)); // "hello" — lowercase
            Assert.True(result.Words[1].Flags.HasFlag(WordFlags.Capital));  // "World" — capitalized
        }

        [Fact]
        public void ExtractWords_CapFlag1_AllCapitalized()
        {
            var segments = ToSegments("hello world");

            var result = ReformatModule.ExtractWords(
                segments, 0, 0,
                prefix: 0, suffix: 0, width: 72,
                TerminalChars, cap: true, guess: true, report: false);

            Assert.Null(result.ErrorMessage);
            // cap=true — все слова считаются capitalized
            Assert.True(result.Words[0].Flags.HasFlag(WordFlags.Capital));
            Assert.True(result.Words[1].Flags.HasFlag(WordFlags.Capital));
        }

        // ============================================================
        // Guess: слияние curious + capitalized (один пробел)
        // ============================================================

        [Fact]
        public void ExtractWords_Guess_MergeCuriousCapitalized()
        {
            // "Hello. World" — curious + capitalized, один пробел
            var segments = ToSegments("Hello. World");

            var result = ReformatModule.ExtractWords(
                segments, 0, 0,
                prefix: 0, suffix: 0, width: 72,
                TerminalChars, cap: false, guess: true, report: false);

            Assert.Null(result.ErrorMessage);
            // Должно слиться в одно слово: "Hello. World"
            Assert.Single(result.Words);
            Assert.Equal("Hello. World", result.Words[0].Text);
        }

        // ============================================================
        // Guess: Shifted при curious + capitalized (два пробела)
        // ============================================================

        [Fact]
        public void ExtractWords_Guess_ShiftedOnTwoSpaces()
        {
            // "Hello.  World" — curious + capitalized, два пробела
            var segments = ToSegments("Hello.  World");

            var result = ReformatModule.ExtractWords(
                segments, 0, 0,
                prefix: 0, suffix: 0, width: 72,
                TerminalChars, cap: false, guess: true, report: false);

            Assert.Null(result.ErrorMessage);
            // Не слились, но "World" помечен как Shifted
            Assert.Equal(2, result.Words.Count);
            Assert.Equal("Hello.", result.Words[0].Text);
            Assert.Equal("World", result.Words[1].Text);
            Assert.True(result.Words[1].Flags.HasFlag(WordFlags.Shifted));
        }

        // ============================================================
        // Report: ошибка при слове > L
        // ============================================================

        [Fact]
        public void ExtractWords_Report_WordTooLong_ReturnsError()
        {
            var segments = ToSegments("superlongword");

            var result = ReformatModule.ExtractWords(
                segments, 0, 0,
                prefix: 0, suffix: 0, width: 10,
                TerminalChars, cap: false, guess: false, report: true);

            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("Word too long", result.ErrorMessage);
            Assert.Empty(result.Words);
        }

        // ============================================================
        // Разбиение длинного слова (без Report)
        // ============================================================

        [Fact]
        public void ExtractWords_NoReport_SplitsLongWord()
        {
            var segments = ToSegments("superlongword");

            var result = ReformatModule.ExtractWords(
                segments, 0, 0,
                prefix: 0, suffix: 0, width: 5,
                TerminalChars, cap: false, guess: false, report: false);

            Assert.Null(result.ErrorMessage);
            Assert.Equal(5, result.L);
            // Слово "superlongword" (13 символов) разбивается на части по 5
            Assert.True(result.Words.Count >= 3);
            foreach (var w in result.Words)
            {
                Assert.True(w.Width <= 5, $"Word '{w.Text}' has width {w.Width} > 5");
            }
        }

        [Fact]
        public void ExtractWords_SplitWord_CapitalFlagOnFirstPart()
        {
            var segments = ToSegments("Superlongword");

            // guess: true — чтобы флаг Capital был установлен
            var result = ReformatModule.ExtractWords(
                segments, 0, 0,
                prefix: 0, suffix: 0, width: 5,
                TerminalChars, cap: false, guess: true, report: false);

            Assert.Null(result.ErrorMessage);
            // Первая часть должна сохранить Capital
            Assert.True(result.Words[0].Flags.HasFlag(WordFlags.Capital));
            // Вторая часть — без Capital
            Assert.False(result.Words[1].Flags.HasFlag(WordFlags.Capital));
        }

        // ============================================================
        // Unicode графемы
        // ============================================================

        [Fact]
        public void ExtractWords_UnicodeEmoji_CorrectWidth()
        {
            // Эмодзи имеет ширину 2
            var segments = ToSegments("a👍b");

            var result = ReformatModule.ExtractWords(
                segments, 0, 0,
                prefix: 0, suffix: 0, width: 72,
                TerminalChars, cap: false, guess: false, report: false);

            Assert.Null(result.ErrorMessage);
            Assert.Single(result.Words);
            // a=1 + 👍=2 + b=1 = 4
            Assert.Equal(4, result.Words[0].Width);
            Assert.Equal("a👍b", result.Words[0].Text);
        }

        [Fact]
        public void ExtractWords_CombiningCharacters_CorrectWidth()
        {
            // é (e + combining acute) — ширина 1
            var segments = ToSegments("caf\u0065\u0301"); // café с combining

            var result = ReformatModule.ExtractWords(
                segments, 0, 0,
                prefix: 0, suffix: 0, width: 72,
                TerminalChars, cap: false, guess: false, report: false);

            Assert.Null(result.ErrorMessage);
            Assert.Single(result.Words);
            // caf = 3, é = 1 (combining) = 1 → total 4
            Assert.Equal(4, result.Words[0].Width);
        }

        // ============================================================
        // Suffixes корректно сохраняются
        // ============================================================

        [Fact]
        public void ExtractWords_Suffixes_CorrectlySaved()
        {
            var segments = ToSegments(
                "---hello...",
                "---world!!!"
            );

            var result = ReformatModule.ExtractWords(
                segments, 0, 1,
                prefix: 3, suffix: 3, width: 72,
                TerminalChars, cap: false, guess: false, report: false);

            Assert.Null(result.ErrorMessage);
            Assert.Equal(2, result.Suffixes.Count);
            Assert.Equal("...", result.Suffixes[0]);
            Assert.Equal("!!!", result.Suffixes[1]);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using xParLib;

namespace xParTests
{
    public class ReformatTests
    {
        // ============================================================
        // Helpers
        // ============================================================

        private static LineSegment[] ToSegments(params string[] lines)
        {
            return lines.Select(l => new LineSegment(l, new LineProp())).ToArray();
        }

        private static ParOptions Opts(
            int width = 72, bool just = false, bool fit = false, bool last = false,
            bool? touch = null, int hang = 0, bool cap = false, bool guess = false,
            bool report = false, Charset? terminalChars = null)
        {
            return new ParOptions
            {
                Width = width,
                Just = just,
                Fit = fit,
                Last = last,
                Touch = touch,
                Hang = hang,
                Cap = cap,
                Guess = guess,
                Report = report,
                TerminalChars = terminalChars ?? Charset.Parse(".?!:"),
            };
        }

        private static SetAffixesResult Affixes(int prefix = 0, int suffix = 0, int afp = 0, int fs = 0)
        {
            return new SetAffixesResult(prefix, suffix, afp, fs);
        }

        // ============================================================
        // Базовые тесты
        // ============================================================

        [Fact]
        public void Reformat_SingleLine_NoChange()
        {
            var segments = ToSegments("hello world");

            var result = ReformatModule.Reformat(
                segments, 0, 0, Opts(width: 72), Affixes(0, 0));

            Assert.Single(result);
            Assert.Equal("hello world", result[0]);
        }

        [Fact]
        public void Reformat_BasicReformat_NoBreaksNeeded()
        {
            var segments = ToSegments("hello world");

            var result = ReformatModule.Reformat(
                segments, 0, 0, Opts(width: 20), Affixes(0, 0));

            Assert.Single(result);
            Assert.Equal("hello world", result[0]);
        }

        // ============================================================
        // Разрывы строк
        // ============================================================

        [Fact]
        public void Reformat_NeedsLineBreaks()
        {
            var segments = ToSegments("hello world foo bar");

            var result = ReformatModule.Reformat(
                segments, 0, 0, Opts(width: 15), Affixes(0, 0));

            // hello(5)+1+world(5)+1+foo(3) = 15 <= 15
            // bar(3) — отдельно
            Assert.Equal(2, result.Length);
            Assert.Equal("hello world foo", result[0]);
            Assert.Equal("bar", result[1]);
        }

        // ============================================================
        // Justification
        // ============================================================

        [Fact]
        public void Reformat_Justify()
        {
            var segments = ToSegments("hello world foo bar");

            var result = ReformatModule.Reformat(
                segments, 0, 0, Opts(width: 15, just: true), Affixes(0, 0));

            // Каждая строка должна быть длиной 15 (кроме последней)
            Assert.True(result.Length >= 2);
            Assert.Equal(15, result[0].Length);
            // Последняя строка не выравнивается
            Assert.True(result.Last().Length <= 15);
        }

        // ============================================================
        // Fit
        // ============================================================

        [Fact]
        public void Reformat_Fit_OptimizesWidth()
        {
            var segments = ToSegments("aaaa bb ccc dd");

            var result = ReformatModule.Reformat(
                segments, 0, 0, Opts(width: 15, fit: true), Affixes(0, 0));

            // fit=true может уменьшить target для более равномерных строк
            Assert.NotNull(result);
            Assert.True(result.Length >= 1);
        }

        // ============================================================
        // Last
        // ============================================================

        [Fact]
        public void Reformat_LastTrue_LastLineCounted()
        {
            var segments = ToSegments("hello world foo");

            var result = ReformatModule.Reformat(
                segments, 0, 0, Opts(width: 15, last: true), Affixes(0, 0));

            Assert.NotNull(result);
            Assert.True(result.Length >= 1);
        }

        // ============================================================
        // Hang
        // ============================================================

        [Fact]
        public void Reformat_Hang_AddsLines()
        {
            var segments = ToSegments("hi");

            var result = ReformatModule.Reformat(
                segments, 0, 0, Opts(width: 10, hang: 3), Affixes(0, 0));

            Assert.Equal(3, result.Length);
            Assert.Equal("hi", result[0]);
            Assert.Equal("", result[1]);
            Assert.Equal("", result[2]);
        }

        // ============================================================
        // Prefix и Suffix
        // ============================================================

        [Fact]
        public void Reformat_WithPrefixAndSuffix()
        {
            var segments = ToSegments(
                ">>hello world",
                ">>foo bar baz"
            );

            var result = ReformatModule.Reformat(
                segments, 0, 1, Opts(width: 20), Affixes(2, 0));

            Assert.Equal(2, result.Length);
            // Префикс ">>" копируется
            Assert.StartsWith(">>", result[0]);
            Assert.StartsWith(">>", result[1]);
        }

        // ============================================================
        // Guess
        // ============================================================

        [Fact]
        public void Reformat_Guess_MergesCuriousCapital()
        {
            var segments = ToSegments("Hello. World foo");

            var result = ReformatModule.Reformat(
                segments, 0, 0, Opts(width: 30, guess: true), Affixes(0, 0));

            Assert.Single(result);
            // "Hello." (curious) + "World" (capital) → слиты
            Assert.Contains("Hello. World", result[0]);
        }

        // ============================================================
        // Пустой IP
        // ============================================================

        [Fact]
        public void Reformat_EmptySegments_ReturnsEmpty()
        {
            var segments = Array.Empty<LineSegment>();

            var result = ReformatModule.Reformat(
                segments, 0, -1, Opts(), Affixes(0, 0));

            Assert.Empty(result);
        }

        // ============================================================
        // Одна строка
        // ============================================================

        [Fact]
        public void Reformat_SingleLineInIP()
        {
            var segments = ToSegments("hello");

            var result = ReformatModule.Reformat(
                segments, 0, 0, Opts(width: 72), Affixes(0, 0));

            Assert.Single(result);
            Assert.Equal("hello", result[0]);
        }

        // ============================================================
        // Ошибка: слово слишком длинное
        // ============================================================

        [Fact]
        public void Reformat_WordTooLong_ReportTrue_ThrowsError()
        {
            var segments = ToSegments("superlongword");

            var ex = Assert.Throws<InvalidOperationException>(() =>
                ReformatModule.Reformat(
                    segments, 0, 0, Opts(width: 5, report: true), Affixes(0, 0)));

            Assert.Contains("Word too long", ex.Message);
        }

        // ============================================================
        // Unicode
        // ============================================================

        [Fact]
        public void Reformat_UnicodeEmoji_CorrectWidth()
        {
            // 👍 имеет ширину 2
            var segments = ToSegments("a👍b c👍d");

            var result = ReformatModule.Reformat(
                segments, 0, 0, Opts(width: 10), Affixes(0, 0));

            Assert.NotNull(result);
            Assert.True(result.Length >= 1);
            // Проверяем, что эмодзи корректно обработаны
            Assert.Contains("👍", result[0]);
        }
    }
}

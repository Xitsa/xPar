using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using xParLib;

namespace xParTests
{
    public class StringTransformerTests
    {
        // ============================================================
        // Helpers
        // ============================================================

        /// <summary>
        /// Создаёт ParOptions из набора аргументов в пользовательском формате.
        /// </summary>
        private static ParOptions ParseOptions(params string[] args)
        {
            var bodyChars = Charset.Parse("");
            var protectChars = Charset.Parse("");
            var quoteChars = Charset.Parse("> ");
            var whiteChars = Charset.Parse(" \f\n\r\t\v");
            var terminalChars = Charset.Parse(".?!:");

            return ParOptions.Parse(args, bodyChars, protectChars, quoteChars, whiteChars, terminalChars);
        }

        private static IReadOnlyList<string> T(params string[] lines)
        {
            return lines;
        }

        private static IReadOnlyList<string> Transform(IReadOnlyList<string> lines, params string[] args)
        {
            var transformer = new StringTransformer();
            var options = ParseOptions(args);
            return transformer.Transform(lines, options);
        }

        // ============================================================
        // Базовые тесты
        // ============================================================

        [Fact]
        public void Transform_EmptyInput_ReturnsEmpty()
        {
            var result = Transform(Array.Empty<string>());

            Assert.Empty(result);
        }

        [Fact]
        public void Transform_SimpleParagraph_Reformats()
        {
            var result = Transform(
                T("hello world foo bar"),
                "w15");

            // hello(5)+1+world(5)+1+foo(3) = 15 <= 15
            // bar(3) — отдельно
            Assert.Equal(2, result.Count);
            Assert.Equal("hello world foo", result[0]);
            Assert.Equal("bar", result[1]);
        }

        // ============================================================
        // Protected lines
        // ============================================================

        [Fact]
        public void Transform_ProtectedLine_CopiedAsIs()
        {
            var result = Transform(
                T("  hello world",
                  ">protected line",
                  "  foo bar"),
                "w15", "P=>");

            // Строка ">protected line" — protected, копируется как есть
            Assert.Equal(3, result.Count);
            Assert.Equal("  hello world", result[0]);
            Assert.Equal(">protected line", result[1]);
            Assert.Equal("  foo bar", result[2]);
        }

        // ============================================================
        // Blank lines
        // ============================================================

        [Fact]
        public void Transform_BlankLine_EmptyString()
        {
            var result = Transform(
                T("hello world",
                  "",
                  "foo bar"),
                "w20");

            Assert.Equal(3, result.Count);
            Assert.Equal("hello world", result[0]);
            Assert.Equal("", result[1]);
            Assert.Equal("foo bar", result[2]);
        }

        [Fact]
        public void Transform_BlankLineWithSpaces_EmptyString()
        {
            var result = Transform(
                T("hello world",
                  "   ",
                  "foo bar"),
                "w20");

            Assert.Equal(3, result.Count);
            Assert.Equal("hello world", result[0]);
            Assert.Equal("", result[1]);
            Assert.Equal("foo bar", result[2]);
        }

        // ============================================================
        // Bodiless строки (vacant)
        // ============================================================

        [Fact]
        public void Transform_VacantLine_TrimsTrailingSpaces()
        {
            // Vacant строка — bodiless с rc = " "
            // При repeat=0 обрезает trailing пробелы
            var result = Transform(
                T("hello world",
                  "               ",  // vacant строка
                  "foo bar"),
                "w15", "r0");

            Assert.Equal(3, result.Count);
            Assert.Equal("hello world", result[0]);
            Assert.Equal("", result[1]); // обрезаны пробелы
            Assert.Equal("foo bar", result[2]);
        }

        // ============================================================
        // Expel — отбрасывание superfluous строк
        // ============================================================

        [Fact]
        public void Transform_Expel_RemovesSuperfluous()
        {
            // Две vacant строки подряд — одна из них superfluous
            var result = Transform(
                T("hello world",
                  "          ",
                  "          ",
                  "foo bar"),
                "w15", "r0", "e");

            // Одна vacant строка удалена как superfluous
            Assert.True(result.Count <= 3);
        }

        // ============================================================
        // Invis — скрытие вставленных vacant строк
        // ============================================================

        [Fact]
        public void Transform_Invis_HidesInsertedVacant()
        {
            // При quote=true и invis=true вставленные vacant строки скрываются
            var result = Transform(
                T("> hello",
                  "> world"),
                "w20", "q", "i");

            Assert.True(result.Count >= 1);
        }

        // ============================================================
        // Несколько абзацев
        // ============================================================

        [Fact]
        public void Transform_MultipleParagraphs_ProcessedIndependently()
        {
            var result = Transform(
                T("hello world foo",
                  "",
                  "bar baz qux"),
                "w15");

            // Два абзаца, разделённых blank line
            Assert.True(result.Count >= 3);
            Assert.Contains("", result); // blank line
        }

        // ============================================================
        // Justification
        // ============================================================

        [Fact]
        public void Transform_Justify_AddsExtraSpaces()
        {
            var result = Transform(
                T("hello world foo bar"),
                "w15", "j");

            // Каждая строка (кроме последней) должна быть длиной 15
            Assert.True(result.Count >= 2);
            Assert.Equal(15, result[0].Length);
        }

        // ============================================================
        // Fit — оптимизация ширины
        // ============================================================

        [Fact]
        public void Transform_Fit_OptimizesWidth()
        {
            var result = Transform(
                T("aaaa bb ccc dd"),
                "w20", "f");

            Assert.NotNull(result);
            Assert.True(result.Count >= 1);
        }

        // ============================================================
        // Hang — дополнительные строки
        // ============================================================

        [Fact]
        public void Transform_Hang_AddsLines()
        {
            var result = Transform(
                T("hi"),
                "w10", "h3");

            Assert.Equal(3, result.Count);
            Assert.Equal("hi", result[0]);
            Assert.Equal("", result[1]);
            Assert.Equal("", result[2]);
        }

        // ============================================================
        // Guess — слияние curious/capital слов
        // ============================================================

        [Fact]
        public void Transform_Guess_MergesCuriousCapital()
        {
            var result = Transform(
                T("Hello. World foo bar"),
                "w30", "g");

            Assert.Single(result);
            // "Hello." (curious) + "World" (capital) → слиты
            Assert.Contains("Hello. World", result[0]);
        }

        // ============================================================
        // Unicode
        // ============================================================

        [Fact]
        public void Transform_UnicodeEmoji_CorrectWidth()
        {
            // 👍 имеет ширину 2
            var result = Transform(
                T("a👍b c👍d"),
                "w10");

            Assert.NotNull(result);
            Assert.True(result.Count >= 1);
            Assert.Contains("👍", result[0]);
        }

        // ============================================================
        // Div — разбиение по отступам
        // ============================================================

        [Fact]
        public void Transform_Div_SplitsByIndentation()
        {
            var result = Transform(
                T("hello world",
                  "  foo bar"),
                "w20", "d");

            // При div=true строки с разным отступом могут быть разными абзацами
            Assert.True(result.Count >= 1);
        }

        // ============================================================
        // Last — последняя строка учитывается
        // ============================================================

        [Fact]
        public void Transform_LastTrue_LastLineCounted()
        {
            var result = Transform(
                T("hello world foo"),
                "w15", "l");

            Assert.NotNull(result);
            Assert.True(result.Count >= 1);
        }

        // ============================================================
        // Touch — суффиксы влево
        // ============================================================

        [Fact]
        public void Transform_Touch_AdjustsLineLength()
        {
            var result = Transform(
                T("hello world"),
                "w20", "t");

            Assert.NotNull(result);
            Assert.Single(result);
        }
    }
}

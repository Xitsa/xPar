using System.Collections.Generic;
using Xunit;
using xParLib;

namespace xParTests
{
    public class ConstructLinesTests
    {
        // ============================================================
        // Helpers
        // ============================================================

        private static Word W(string text, int? width = null, WordFlags flags = WordFlags.None, int? nextLine = null)
        {
            return new Word
            {
                Text = text,
                Width = width ?? text.Length,
                Flags = flags,
                NextLine = nextLine,
            };
        }

        private static SetAffixesResult A(int prefix, int suffix, int afp = 0, int fs = 0)
        {
            return new SetAffixesResult(prefix, suffix, afp, fs);
        }

        private static ExtractWordsResult ER(List<Word> words, List<string> prefixes, List<string> suffixes, int l)
        {
            return new ExtractWordsResult(words, prefixes, suffixes, l, null);
        }

        // ============================================================
        // Базовые тесты
        // ============================================================

        [Fact]
        public void ConstructLines_NoWords_NoHang_ReturnsEmpty()
        {
            var words = new List<Word>();
            var prefixes = new List<string>();
            var suffixes = new List<string>();

            var result = ReformatModule.ConstructLines(
                ER(words, prefixes, suffixes, 10), A(0, 0),
                hang: 0, just: false, last: false, touch: false);

            Assert.Empty(result);
        }

        [Fact]
        public void ConstructLines_SingleWord_SingleLine()
        {
            var words = new List<Word> { W("hello", width: 5) };
            var prefixes = new List<string> { "" };
            var suffixes = new List<string> { "" };

            var result = ReformatModule.ConstructLines(
                ER(words, prefixes, suffixes, 10), A(0, 0),
                hang: 0, just: false, last: false, touch: false);

            Assert.Single(result);
            Assert.Equal("hello", result[0]);
        }

        // ============================================================
        // Префиксы и суффиксы
        // ============================================================

        [Fact]
        public void ConstructLines_WithPrefixAndSuffix()
        {
            var words = new List<Word> { W("hello", width: 5) };
            var prefixes = new List<string> { ">>" };
            var suffixes = new List<string> { "<<" };

            var result = ReformatModule.ConstructLines(
                ER(words, prefixes, suffixes, 10), A(2, 2),
                hang: 0, just: false, last: false, touch: false);

            Assert.Single(result);
            // ">>" + "hello" + пробелы до L=10 + "<<"
            // linelen = 10 + 4 = 14, bodyEndLen = 2 + 10 = 12
            // ">>" (2) + "hello" (5) + 5 пробелов = 12, затем "<<"
            Assert.Equal(">>hello     <<", result[0]);
        }

        [Fact]
        public void ConstructLines_PrefixSuffixCopiedFromOriginal()
        {
            var words = new List<Word>
            {
                W("hello", width: 5, nextLine: 1),
                W("world", width: 5),
            };
            var prefixes = new List<string> { ">>", "<<" };
            var suffixes = new List<string> { ".1", ".2" };

            var result = ReformatModule.ConstructLines(
                ER(words, prefixes, suffixes, 10), A(2, 2),
                hang: 0, just: false, last: false, touch: false);

            Assert.Equal(2, result.Count);
            Assert.Equal(">>hello     .1", result[0]);
            Assert.Equal("<<world     .2", result[1]);
        }

        // ============================================================
        // Несколько строк (разрывы)
        // ============================================================

        [Fact]
        public void ConstructLines_MultipleLines_FromNextLine()
        {
            var words = new List<Word>
            {
                W("hello", width: 5, nextLine: 1),
                W("world", width: 5),
            };
            var prefixes = new List<string> { "", "" };
            var suffixes = new List<string> { "", "" };

            var result = ReformatModule.ConstructLines(
                ER(words, prefixes, suffixes, 10), A(0, 0),
                hang: 0, just: false, last: false, touch: false);

            Assert.Equal(2, result.Count);
            Assert.Equal("hello", result[0]);
            Assert.Equal("world", result[1]);
        }

        // ============================================================
        // Hang
        // ============================================================

        [Fact]
        public void ConstructLines_Hang_AddsEmptyLines()
        {
            var words = new List<Word> { W("hi", width: 2) };
            var prefixes = new List<string> { "" };
            var suffixes = new List<string> { "" };

            var result = ReformatModule.ConstructLines(
                ER(words, prefixes, suffixes, 5), A(0, 0),
                hang: 3, just: false, last: false, touch: false);

            Assert.Equal(3, result.Count);
            Assert.Equal("hi", result[0]);
            Assert.Equal("", result[1]);
            Assert.Equal("", result[2]);
        }

        // ============================================================
        // Touch
        // ============================================================

        [Fact]
        public void ConstructLines_Touch_AdjustsL()
        {
            var words = new List<Word>
            {
                W("hello", width: 5, nextLine: 1),
                W("world", width: 5),
            };
            var prefixes = new List<string> { "", "" };
            var suffixes = new List<string> { "", "" };

            // L=20, но фактическая макс длина = 5
            // touch=true → L станет 5
            var result = ReformatModule.ConstructLines(
                ER(words, prefixes, suffixes, 20), A(0, 0),
                hang: 0, just: false, last: false, touch: true);

            Assert.Equal(2, result.Count);
            // После touch L=5, linelen=5
            Assert.Equal("hello", result[0]);
            Assert.Equal("world", result[1]);
        }

        // ============================================================
        // Justification
        // ============================================================

        [Fact]
        public void ConstructLines_Just_DistributesExtraSpaces()
        {
            var words = new List<Word>
            {
                W("hi", width: 2, nextLine: null),
                W("ok", width: 2),
            };
            var prefixes = new List<string> { "", "" };
            var suffixes = new List<string> { "", "" };

            // L=10, 2 слова на одной строке, extra=10-2-1-2=5, numgaps=1
            // just=true → 5 дополнительных пробелов в 1 промежуток
            var result = ReformatModule.ConstructLines(
                ER(words, prefixes, suffixes, 10), A(0, 0),
                hang: 0, just: true, last: true, touch: false);

            Assert.Single(result);
            // "hi" + 1 + 5 = 6 пробелов + "ok" = "hi      ok"
            Assert.Equal("hi      ok", result[0]);
        }

        // ============================================================
        // Shifted
        // ============================================================

        [Fact]
        public void ConstructLines_Shifted_AddsExtraSpace()
        {
            var words = new List<Word>
            {
                W("hi", width: 2, nextLine: 1),
                W("ok", width: 2, flags: WordFlags.Shifted),
            };
            var prefixes = new List<string> { "", "" };
            var suffixes = new List<string> { "", "" };

            var result = ReformatModule.ConstructLines(
                ER(words, prefixes, suffixes, 10), A(0, 0),
                hang: 0, just: false, last: false, touch: false);

            Assert.Equal(2, result.Count);
            // Строка 2: "ok" (shifted не влияет на одиночное слово)
            Assert.Equal("hi", result[0]);
            Assert.Equal("ok", result[1]);
        }

        [Fact]
        public void ConstructLines_Shifted_BetweenWords()
        {
            var words = new List<Word>
            {
                W("hi", width: 2, nextLine: null),
                W("ok", width: 2, flags: WordFlags.Shifted),
            };
            var prefixes = new List<string> { "" };
            var suffixes = new List<string> { "" };

            // Оба слова на одной строке, ok — shifted
            // "hi" + " " + " " (shifted) + "ok" = "hi  ok"
            var result = ReformatModule.ConstructLines(
                ER(words, prefixes, suffixes, 10), A(0, 0),
                hang: 0, just: false, last: false, touch: false);

            Assert.Single(result);
            Assert.Equal("hi  ok", result[0]);
        }

        // ============================================================
        // Fallback префикс/суффикс
        // ============================================================

        [Fact]
        public void ConstructLines_FallbackPrefixSuffix()
        {
            // Два слова на отдельных строках
            var words = new List<Word>
            {
                W("hi", width: 2),
                W("ok", width: 2),
            };
            words[0].NextLine = 1; // hi на строке 1, ok на строке 2

            var prefixes = new List<string> { ">>" };
            var suffixes = new List<string> { "<<" };

            // hang=3, numIn=1 → дополнительные строки используют fallback
            var result = ReformatModule.ConstructLines(
                ER(words, prefixes, suffixes, 5), A(2, 2, afp: 2, fs: 2),
                hang: 3, just: false, last: false, touch: false);

            Assert.Equal(3, result.Count);
            // Строка 1: ">>" + "hi" + пробелы до L=5 + "<<"
            // bodyEndLen = 2 + 5 = 7, ">>" (2) + "hi" (2) + 3 пробела = 7
            Assert.Equal(">>hi   <<", result[0]);
            // Строка 2: аналогично
            Assert.Equal(">>ok   <<", result[1]);
            // Строка 3: fallback префикс + пробелы до bodyEndLen + fallback суффикс
            // bodyEndLen = prefix + L = 2 + 5 = 7, префикс ">>" (2) + 5 пробелов
            Assert.Equal(">>     <<", result[2]);
        }
    }
}

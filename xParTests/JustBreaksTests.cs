using System.Collections.Generic;
using Xunit;
using xParLib;

namespace xParTests
{
    public class JustBreaksTests
    {
        // ============================================================
        // Helpers
        // ============================================================

        private static Word W(string text, int? width = null, WordFlags flags = WordFlags.None)
        {
            return new Word
            {
                Text = text,
                Width = width ?? text.Length,
                Flags = flags,
            };
        }

        // ============================================================
        // Базовые тесты
        // ============================================================

        [Fact]
        public void JustBreaks_EmptyList_ReturnsNull()
        {
            var words = new List<Word>();

            string? result = ReformatModule.JustBreaks(words, L: 10, last: false);

            Assert.Null(result);
        }

        [Fact]
        public void JustBreaks_SingleWord_ReturnsNull()
        {
            var words = new List<Word> { W("hello", width: 5) };

            string? result = ReformatModule.JustBreaks(words, L: 10, last: false);

            Assert.Null(result);
            Assert.Null(words[0].NextLine);
        }

        // ============================================================
        // Все слова на одной строке
        // ============================================================

        [Fact]
        public void JustBreaks_AllWordsFit_NoBreaks()
        {
            var words = new List<Word>
            {
                W("hi", width: 2),
                W("ok", width: 2),
            };
            // 2 + 1 + 2 = 5 <= 10, extra = 5, numgaps = 1, gap = 5

            string? result = ReformatModule.JustBreaks(words, L: 10, last: true);

            Assert.Null(result);
            Assert.Null(words[0].NextLine);
            Assert.Null(words[1].NextLine);
        }

        // ============================================================
        // Нужен разрыв
        // ============================================================

        [Fact]
        public void JustBreaks_NeedsBreak_CorrectNextLine()
        {
            var words = new List<Word>
            {
                W("hello", width: 5),
                W("world", width: 5),
                W("foo", width: 3),
            };
            // L = 12
            // hello(5)+1+world(5)=11<=12 → могут быть вместе
            // world(5)+1+foo(3)=9<=12 → могут быть вместе
            // hello(5)+1+world(5)+1+foo(3)=14>12 → не все вместе

            string? result = ReformatModule.JustBreaks(words, L: 12, last: false);

            Assert.Null(result);
            // hello→world+foo: extra=1, gap=1. hello отдельно: extra=7, gap=7>1.
            // Оптимально: hello→world, foo отдельно? Или hello+world, foo отдельно?
            // Фактический результат:
            Assert.Equal(2, words[0].NextLine); // hello → foo
            Assert.Null(words[1].NextLine);      // world+foo на одной строке
            Assert.Null(words[2].NextLine);       // foo — последняя
        }

        // ============================================================
        // Невозможно выровнять (maxgap >= L)
        // ============================================================

        [Fact]
        public void JustBreaks_CannotJustify_ReturnsError()
        {
            // Одно длинное слово + одно короткое, L слишком мал
            var words = new List<Word>
            {
                W("a", width: 1),
                W("b", width: 1),
            };
            // L = 1
            // a(1)+1+b(1)=3>1 → разрыв
            // Фаза 1: b → score=1, nextline=null.
            //   a: extra=0, j=1: gap=L=1, score=1. 1<1? Нет.
            //   a: j=2: extra=-1<0 → стоп.
            //   a: score=L=1. maxgap=1 >= L=1 → ошибка.

            string? result = ReformatModule.JustBreaks(words, L: 1, last: false);

            Assert.NotNull(result);
            Assert.Contains("Cannot justify", result);
        }

        // ============================================================
        // last=false vs last=true
        // ============================================================

        [Fact]
        public void JustBreaks_LastFalse_LastLineNoExtraSpaces()
        {
            var words = new List<Word>
            {
                W("hello", width: 5),
                W("world", width: 5),
            };
            // L = 15, last=false
            // hello(5)+1+world(5)=11<=15 → все на одной строке
            // last=false → gap=0 для последней строки

            string? result = ReformatModule.JustBreaks(words, L: 15, last: false);

            Assert.Null(result);
            Assert.Null(words[0].NextLine);
            Assert.Equal(0, words[0].Score); // last=false → score=0
        }

        [Fact]
        public void JustBreaks_LastTrue_LastLineJustified()
        {
            var words = new List<Word>
            {
                W("hello", width: 5),
                W("world", width: 5),
            };
            // L = 15, last=true
            // hello(5)+1+world(5)=11<=15 → все на одной строке
            // extra=4, numgaps=1, gap=4

            string? result = ReformatModule.JustBreaks(words, L: 15, last: true);

            Assert.Null(result);
            // Все слова на одной строке
            Assert.Null(words[0].NextLine);
            // Фаза 2: extra=4, baseGap=4, numbiggaps=0, score=4*(4+0)+0=16
            Assert.Equal(16, words[0].Score);
        }

        // ============================================================
        // Shifted-слова
        // ============================================================

        [Fact]
        public void JustBreaks_ShiftedWord_AccountsExtraSpace()
        {
            var words = new List<Word>
            {
                W("hello", width: 5),
                W("world", width: 5, flags: WordFlags.Shifted),
            };
            // L = 12
            // hello(5) + 1 + shifted(1) + world(5) = 12 <= 12
            // extra = 0, numgaps = 1, gap = 0

            string? result = ReformatModule.JustBreaks(words, L: 12, last: true);

            Assert.Null(result);
            Assert.Null(words[0].NextLine);
        }

        // ============================================================
        // Равномерное распределение пробелов
        // ============================================================

        [Fact]
        public void JustBreaks_EvenDistributionOfSpaces()
        {
            var words = new List<Word>
            {
                W("aaa", width: 3),
                W("bb", width: 2),
                W("c", width: 1),
            };
            // L = 10
            // 3 + 1 + 2 + 1 + 1 = 8 <= 10 → все на одной строке
            // extra = 2, numgaps = 2
            // Фаза 1: maxgap = ceil(2/2) = 1
            // Фаза 2: baseGap = 1, numbiggaps = 0
            // score = 1 * (2 + 0) + 0 = 2

            string? result = ReformatModule.JustBreaks(words, L: 10, last: true);

            Assert.Null(result);
            Assert.Null(words[0].NextLine);
            // Фаза 1: maxgap = 1
            // Фаза 2: score = 2
            Assert.Equal(2, words[0].Score);
        }

        // ============================================================
        // Несколько строк, justification
        // ============================================================

        [Fact]
        public void JustBreaks_MultipleLines_Justified()
        {
            var words = new List<Word>
            {
                W("aaaa", width: 4),
                W("bb", width: 2),
                W("ccc", width: 3),
                W("dd", width: 2),
            };
            // L = 7
            // aaaa(4)+1+bb(2)=7 <= 7 → строка 1
            // ccc(3)+1+dd(2)=6 <= 7 → строка 2
            // extra для строки 1: 7-7=0, numgaps=1, gap=0
            // extra для строки 2: 7-6=1, numgaps=1, gap=1

            string? result = ReformatModule.JustBreaks(words, L: 7, last: true);

            Assert.Null(result);
            Assert.Equal(2, words[0].NextLine); // aaaa → ccc
            Assert.Null(words[2].NextLine);      // ccc → dd (одна строка)
        }

        // ============================================================
        // Формула суммы квадратов
        // ============================================================

        [Fact]
        public void JustBreaks_SumOfSquaresFormula()
        {
            var words = new List<Word>
            {
                W("a", width: 1),
                W("b", width: 1),
                W("c", width: 1),
            };
            // L = 8
            // 1 + 1 + 1 + 1 + 1 = 5 <= 8, extra = 3, numgaps = 2
            // Фаза 1: maxgap = ceil(3/2) = 2
            // Фаза 2: baseGap = 3/2 = 1, numbiggaps = 1
            // score = 1 * (3 + 1) + 1 = 5
            // Проверка: 1 промежуток с 2 пробелами, 1 с 1 → 2² + 1² = 4+1 = 5 ✓

            string? result = ReformatModule.JustBreaks(words, L: 8, last: true);

            Assert.Null(result);
            Assert.Null(words[0].NextLine);
            // Фаза 1: maxgap = 2
            // Фаза 2: score = 5
            Assert.Equal(5, words[0].Score);
        }

        // ============================================================
        // Невозможная конфигурация (impossibility 3)
        // ============================================================

        [Fact]
        public void JustBreaks_ImpossibleConfig_ReturnsError()
        {
            // Слова, которые не могут быть выровнены ни при каком разбиении
            var words = new List<Word>
            {
                W("longword", width: 8),
                W("x", width: 1),
            };
            // L = 5, слово 8 > 5

            string? result = ReformatModule.JustBreaks(words, L: 5, last: false);

            Assert.NotNull(result);
        }
    }
}

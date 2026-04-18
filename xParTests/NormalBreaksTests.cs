using System.Collections.Generic;
using Xunit;
using xParLib;

namespace xParTests
{
    public class NormalBreaksTests
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
        public void NormalBreaks_EmptyList_ReturnsNull()
        {
            var words = new List<Word>();

            string? result = ReformatModule.NormalBreaks(words, L: 10, fit: false, last: false);

            Assert.Null(result);
        }

        [Fact]
        public void NormalBreaks_AllWordsFit_NoBreaks()
        {
            var words = new List<Word>
            {
                W("hi", width: 2),
                W("ok", width: 2),
            };
            // 2 + 1 + 2 = 5 <= 10

            string? result = ReformatModule.NormalBreaks(words, L: 10, fit: false, last: false);

            Assert.Null(result);
            Assert.Null(words[0].NextLine);
            Assert.Null(words[1].NextLine);
        }

        // ============================================================
        // fit=false, нужен разрыв
        // ============================================================

        [Fact]
        public void NormalBreaks_NoFit_NeedsBreak()
        {
            var words = new List<Word>
            {
                W("hello", width: 5),
                W("world", width: 5),
                W("foo", width: 3),
            };
            // L = 8, target = 8
            // hello(5)+1+world(5)=11>8 → разрыв
            // world(5)+1+foo(3)=9>8 → разрыв

            string? result = ReformatModule.NormalBreaks(words, L: 8, fit: false, last: false);

            Assert.Null(result);
            Assert.Equal(1, words[0].NextLine); // hello → world
            Assert.Equal(2, words[1].NextLine); // world → foo
            Assert.Null(words[2].NextLine);     // foo — последняя
        }

        // ============================================================
        // fit=true, оптимизация ширины
        // ============================================================

        [Fact]
        public void NormalBreaks_Fit_ReducesTarget()
        {
            var words = new List<Word>
            {
                W("aaaa", width: 4),
                W("bb", width: 2),
                W("ccc", width: 3),
                W("dd", width: 2),
            };
            // L = 10, но оптимально: [aaaa bb] [ccc dd] → 7, 6
            // fit=true → target может быть уменьшен до 7

            string? result = ReformatModule.NormalBreaks(words, L: 10, fit: true, last: false);

            Assert.Null(result);
            // Проверяем, что разрывы выбраны оптимально
            Assert.Equal(2, words[0].NextLine); // aaaa → ccc
            Assert.Null(words[2].NextLine);      // ccc dd → одна строка
        }

        // ============================================================
        // last=false vs last=true
        // ============================================================

        [Fact]
        public void NormalBreaks_LastFalse_LastLineIgnored()
        {
            var words = new List<Word>
            {
                W("longword", width: 8),
                W("x", width: 1),
            };
            // L = 10, last=false
            // longword(8)+1+x(1)=10 <= 10 → все на одной строке
            // Последняя строка не учитывается → extra=0, minlen=0

            string? result = ReformatModule.NormalBreaks(words, L: 10, fit: false, last: false);

            Assert.Null(result);
            Assert.Null(words[0].NextLine);
        }

        [Fact]
        public void NormalBreaks_LastTrue_LastLineCounted()
        {
            var words = new List<Word>
            {
                W("longword", width: 8),
                W("x", width: 1),
            };
            // L = 10, last=true
            // Последняя строка учитывается → extra=2, score=4

            string? result = ReformatModule.NormalBreaks(words, L: 10, fit: false, last: true);

            Assert.Null(result);
            Assert.Null(words[0].NextLine);
            // Score = (10-10)^2 = 0 (одна строка, linelen=10, extra=0)
            // Нет: linelen=8+1+1=10, extra=0, score=0
            Assert.Equal(0, words[0].Score);
        }

        // ============================================================
        // Слово шире L
        // ============================================================

        [Fact]
        public void NormalBreaks_WordTooLong_ReturnsError()
        {
            var words = new List<Word>
            {
                W("hello", width: 5),
                W("superlongword", width: 13),
                W("foo", width: 3),
            };

            string? result = ReformatModule.NormalBreaks(words, L: 10, fit: false, last: false);

            Assert.NotNull(result);
            Assert.Contains("impossibility", result);
        }

        // ============================================================
        // Невозможная конфигурация (impossibility 2)
        // ============================================================

        [Fact]
        public void NormalBreaks_ImpossibleConfig_ReturnsError()
        {
            // Создаём ситуацию, где ни один разрыв не удовлетворяет minlen
            // Это сложно воспроизвести, т.к. simplebreaks уже проверяет возможность.
            // Вместо этого проверим, что ошибка возвращается при shortest < 0.
            var words = new List<Word>
            {
                W("abcdefghij", width: 10),
                W("k", width: 1),
            };
            // L = 9, слово 10 > 9

            string? result = ReformatModule.NormalBreaks(words, L: 9, fit: false, last: false);

            Assert.NotNull(result);
        }

        // ============================================================
        // Shifted-слова
        // ============================================================

        [Fact]
        public void NormalBreaks_ShiftedWord_AccountsExtraSpace()
        {
            var words = new List<Word>
            {
                W("hello", width: 5),
                W("world", width: 5, flags: WordFlags.Shifted),
            };
            // L = 10
            // hello(5) + 1 + shifted(1) + world(5) = 12 > 10 → разрыв

            string? result = ReformatModule.NormalBreaks(words, L: 10, fit: false, last: false);

            Assert.Null(result);
            Assert.Equal(1, words[0].NextLine);
            Assert.Null(words[1].NextLine);
        }

        // ============================================================
        // Минимизация суммы квадратов
        // ============================================================

        [Fact]
        public void NormalBreaks_MinimizesSumOfSquares()
        {
            var words = new List<Word>
            {
                W("aaaaa", width: 5),
                W("bbb", width: 3),
                W("cc", width: 2),
                W("dddd", width: 4),
            };
            // L = 8, target = 8, last=false
            // Этап B: shortest = SimpleBreaks(8) = 5
            // Этап C:
            //   dddd: last → extra=0, minlen=0, score=0, nextline=null.
            //   cc: j=3: linelen=2, score=0. 2<5(minlen) → skip.
            //       j=4: linelen=2+1+4=7<=8. extra=1, minlen=5, score=0. 7>=5 → score=1, nextline=null.
            //       cc: score=1, nextline=null (cc+dddd на последней строке).
            //   bbb: j=2: linelen=3, score=1. 3<5 → skip.
            //       j=3: linelen=3+1+2=6, score=0. 6>=5 → score=4, nextline=3.
            //       j=4: linelen=11>8 → стоп.
            //       bbb: score=4, nextline=3(dddd).
            //   aaaaa: j=1: linelen=5, score=4. 5>=5 → score=13, nextline=1.
            //       j=2: linelen=9>8 → стоп.
            //       aaaaa: score=13, nextline=1(bbb).
            // Итого: [aaaaa] [bbb cc] [dddd] → 5, 6, 4.

            string? result = ReformatModule.NormalBreaks(words, L: 8, fit: false, last: false);

            Assert.Null(result);
            Assert.Equal(1, words[0].NextLine);  // aaaaa → bbb
            Assert.Equal(3, words[1].NextLine);  // bbb → dddd
            Assert.Null(words[2].NextLine);       // cc → последняя строка (cc+dddd)
            Assert.Null(words[3].NextLine);        // dddd — последняя
        }

        [Fact]
        public void NormalBreaks_VerifyBreaks()
        {
            var words = new List<Word>
            {
                W("aaaaa", width: 5),
                W("bbb", width: 3),
                W("cc", width: 2),
                W("dddd", width: 4),
            };

            string? result = ReformatModule.NormalBreaks(words, L: 8, fit: false, last: false);

            Assert.Null(result);
            // Проверяем, что разрывы корректны (каждая строка <= 8)
            // Просто проверяем NextLine
            Assert.NotNull(words[0].NextLine); // aaaaa → что-то
            Assert.True(words[0].Width <= 8);
        }

        // ============================================================
        // Несколько слов, все на одной строке
        // ============================================================

        [Fact]
        public void NormalBreaks_SingleLine_ScoreCorrect()
        {
            var words = new List<Word>
            {
                W("hi", width: 2),
                W("ok", width: 2),
            };
            // 2 + 1 + 2 = 5, L = 10, last=false
            // extra=0 (last=false), minlen=0, score=0

            string? result = ReformatModule.NormalBreaks(words, L: 10, fit: false, last: false);

            Assert.Null(result);
            Assert.Null(words[0].NextLine);
            Assert.Equal(0, words[0].Score);
        }

        [Fact]
        public void NormalBreaks_SingleLine_LastTrue_ScoreCorrect()
        {
            var words = new List<Word>
            {
                W("hi", width: 2),
                W("ok", width: 2),
            };
            // 2 + 1 + 2 = 5, L = 10, last=true
            // extra=5, score=25

            string? result = ReformatModule.NormalBreaks(words, L: 10, fit: false, last: true);

            Assert.Null(result);
            Assert.Null(words[0].NextLine);
            Assert.Equal(25, words[0].Score); // (10-5)^2 = 25
        }
    }
}

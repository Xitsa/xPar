using System.Collections.Generic;
using Xunit;
using xParLib;

namespace xParTests
{
    public class SimpleBreaksTests
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
        public void SimpleBreaks_EmptyList_ReturnsL()
        {
            var words = new List<Word>();

            int result = ReformatModule.SimpleBreaks(words, L: 10, last: false);

            Assert.Equal(10, result);
        }

        [Fact]
        public void SimpleBreaks_SingleWordFits_LastFalse_ReturnsL()
        {
            var words = new List<Word> { W("hello", width: 5) };

            int result = ReformatModule.SimpleBreaks(words, L: 10, last: false);

            Assert.Equal(10, result); // last=false → score = L
            Assert.Null(words[0].NextLine);
        }

        [Fact]
        public void SimpleBreaks_SingleWordFits_LastTrue_ReturnsLinelen()
        {
            var words = new List<Word> { W("hello", width: 5) };

            int result = ReformatModule.SimpleBreaks(words, L: 10, last: true);

            Assert.Equal(5, result); // last=true → score = linelen
            Assert.Null(words[0].NextLine);
        }

        // ============================================================
        // Все слова на одной строке
        // ============================================================

        [Fact]
        public void SimpleBreaks_AllWordsFit_LastFalse_AllNextLineNull()
        {
            var words = new List<Word>
            {
                W("hello", width: 5),
                W("world", width: 5),
            };
            // 5 + 1 + 5 = 11 > 10, не влезут. Уменьшим:
            var words2 = new List<Word>
            {
                W("hi", width: 2),
                W("ok", width: 2),
            };
            // 2 + 1 + 2 = 5 <= 10

            int result = ReformatModule.SimpleBreaks(words2, L: 10, last: false);

            Assert.Equal(10, result);
            Assert.Null(words2[0].NextLine);
            Assert.Null(words2[1].NextLine);
        }

        [Fact]
        public void SimpleBreaks_AllWordsFit_LastTrue_ReturnsTotalLength()
        {
            var words = new List<Word>
            {
                W("hi", width: 2),
                W("ok", width: 2),
            };
            // 2 + 1 + 2 = 5

            int result = ReformatModule.SimpleBreaks(words, L: 10, last: true);

            Assert.Equal(5, result);
        }

        // ============================================================
        // Нужен разрыв
        // ============================================================

        [Fact]
        public void SimpleBreaks_NeedsBreak_CorrectNextLine()
        {
            var words = new List<Word>
            {
                W("hello", width: 5),
                W("world", width: 5),
                W("foo", width: 3),
            };
            // L = 8
            // Фаза 1: foo(3) → score=8(last=false), nextline=null.
            //   world(5)+1+foo(3)=9>8 → не влезает.
            // Фаза 2: world → score=min(5,8)=5, nextline=2(foo).
            //   hello → score=min(5,5)=5, nextline=1(world).

            int result = ReformatModule.SimpleBreaks(words, L: 8, last: false);

            Assert.Equal(5, result);
            Assert.Equal(1, words[0].NextLine); // hello → world
            Assert.Equal(2, words[1].NextLine); // world → foo
            Assert.Null(words[2].NextLine);     // foo — последняя
        }

        [Fact]
        public void SimpleBreaks_MaximizesShortestLine()
        {
            var words = new List<Word>
            {
                W("aaaa", width: 4),
                W("bb", width: 2),
                W("ccc", width: 3),
                W("dd", width: 2),
            };
            // L = 7, last=false
            // Фаза 1: dd(2) → score=7, nextline=null.
            //   ccc(3)+1+dd(2)=6<=7 → score=7, nextline=null.
            //   bb(2)+1+ccc(3)+1+dd(2)=9>7 → стоп, idx=1.
            // Фаза 2: bb → j=2: linelen=2, score=min(2,7)=2, nextline=2.
            //   j=3: linelen=2+1+3=6, score=min(6,7)=6≥2 → nextline=3, score=6.
            //   bb: score=6, nextline=3.
            //   aaaa → j=1: linelen=4, score=min(4,6)=4, nextline=1.
            //   j=2: linelen=4+1+2=7, score=min(7,7)=7≥4 → nextline=2, score=7.
            //   aaaa: score=7, nextline=2.
            // Строки: [aaaa bb] [ccc dd] → 7, 6. last=false → последняя не считается → result=7.

            int result = ReformatModule.SimpleBreaks(words, L: 7, last: false);

            Assert.Equal(7, result);
            Assert.Equal(2, words[0].NextLine); // aaaa → следующая строка начинается с ccc
            // words[1].NextLine = 3 — промежуточное значение DP (bb не является началом строки)
            Assert.Null(words[2].NextLine);      // ccc dd → одна строка
            Assert.Null(words[3].NextLine);       // dd — последняя
        }

        // ============================================================
        // Слово шире L
        // ============================================================

        [Fact]
        public void SimpleBreaks_WordTooLong_ReturnsMinus1()
        {
            var words = new List<Word>
            {
                W("hello", width: 5),
                W("superlongword", width: 13),
                W("foo", width: 3),
            };

            int result = ReformatModule.SimpleBreaks(words, L: 10, last: false);

            Assert.Equal(-1, result);
        }

        // ============================================================
        // Shifted-слова
        // ============================================================

        [Fact]
        public void SimpleBreaks_ShiftedWord_AccountsExtraSpace()
        {
            var words = new List<Word>
            {
                W("hello", width: 5),
                W("world", width: 5, flags: WordFlags.Shifted),
            };
            // L = 10
            // hello(5) + 1 + shifted(1) + world(5) = 12 > 10 → разрыв

            int result = ReformatModule.SimpleBreaks(words, L: 10, last: false);

            Assert.Equal(5, result); // обе строки по 5
            Assert.Equal(1, words[0].NextLine);
            Assert.Null(words[1].NextLine);
        }

        [Fact]
        public void SimpleBreaks_ShiftedWord_FitsWithExtraSpace()
        {
            var words = new List<Word>
            {
                W("hi", width: 2),
                W("ok", width: 2, flags: WordFlags.Shifted),
            };
            // L = 6
            // hi(2) + 1 + shifted(1) + ok(2) = 6 <= 6 → помещаются

            int result = ReformatModule.SimpleBreaks(words, L: 6, last: true);

            Assert.Equal(6, result);
            Assert.Null(words[0].NextLine);
            Assert.Null(words[1].NextLine);
        }

        // ============================================================
        // Несколько строк, проверка score
        // ============================================================

        [Fact]
        public void SimpleBreaks_MultipleLines_CorrectScores()
        {
            var words = new List<Word>
            {
                W("aaaaa", width: 5),
                W("bbb", width: 3),
                W("cc", width: 2),
                W("dddd", width: 4),
            };
            // L = 8
            // Фаза 1: dddd(4) → score=8, nextline=null.
            //   cc(2)+1+dddd(4)=7<=8 → score=7, nextline=null.
            //   bbb(3)+1+cc(2)+1+dddd(4)=11>8 → стоп, idx=1.
            // Фаза 2: bbb → j=2: linelen=3, score=min(3,7)=3. j=3: linelen=6, score=min(6,8)=6≥3 → nextline=3, score=6.
            //   aaaaa → j=1: linelen=5, score=min(5,6)=5. j=2: linelen=9>8 → стоп. nextline=1, score=5.
            // Итого: [aaaaa] [bbb dddd] [cc dddd] — нет: [aaaaa] [bbb→dddd] [cc dddd]
            // Строки: [aaaaa] [bbb dddd] — 5, 3+1+4=8. cc+dddd=7.
            // result = 5.

            int result = ReformatModule.SimpleBreaks(words, L: 8, last: false);

            Assert.Equal(5, result);
            Assert.Equal(1, words[0].NextLine);  // aaaaa → bbb
            Assert.Equal(3, words[1].NextLine);  // bbb → dddd
            Assert.Null(words[2].NextLine);       // cc dddd → одна строка
            Assert.Null(words[3].NextLine);        // dddd — последняя
        }

        // ============================================================
        // Last=true vs last=false
        // ============================================================

        [Fact]
        public void SimpleBreaks_LastTrue_IncludesLastLineInMin()
        {
            var words = new List<Word>
            {
                W("hello", width: 5),
                W("world", width: 5),
            };
            // L = 10, каждое слово отдельно
            // last=false: min(5, 10) = 5, но последняя не считается → min=5
            // last=true: min(5, 5) = 5

            var words1 = new List<Word>
            {
                W("hello", width: 5),
                W("world", width: 5),
            };
            int resultLastFalse = ReformatModule.SimpleBreaks(words1, L: 10, last: false);

            var words2 = new List<Word>
            {
                W("hello", width: 5),
                W("world", width: 5),
            };
            int resultLastTrue = ReformatModule.SimpleBreaks(words2, L: 10, last: true);

            // В данном случае обе строки по 5, разницы нет
            Assert.Equal(5, resultLastFalse);
            Assert.Equal(5, resultLastTrue);
        }

        [Fact]
        public void SimpleBreaks_LastFalse_LastLineIgnored()
        {
            var words = new List<Word>
            {
                W("longword", width: 8),
                W("x", width: 1),
            };
            // L = 10
            // longword(8) + 1 + x(1) = 10 <= 10 → все на одной строке
            // last=false → score = L = 10
            // last=true → score = 10

            var words1 = new List<Word>
            {
                W("longword", width: 8),
                W("x", width: 1),
            };
            int resultLastFalse = ReformatModule.SimpleBreaks(words1, L: 10, last: false);

            var words2 = new List<Word>
            {
                W("longword", width: 8),
                W("x", width: 1),
            };
            int resultLastTrue = ReformatModule.SimpleBreaks(words2, L: 10, last: true);

            Assert.Equal(10, resultLastFalse);
            Assert.Equal(10, resultLastTrue);
        }

        // ============================================================
        // Unicode-слова
        // ============================================================

        [Fact]
        public void SimpleBreaks_UnicodeEmoji_CorrectWidth()
        {
            var words = new List<Word>
            {
                W("a👍b", width: 4), // a=1 + 👍=2 + b=1
                W("cd", width: 2),
            };
            // L = 5
            // Фаза 1: cd(2) → score=5, nextline=null.
            //   a👍b(4)+1+cd(2)=7>5 → стоп, idx=0.
            // Фаза 2: a👍b → j=1: linelen=4, score=min(4,5)=4. nextline=1, score=4.
            // result = 4.

            int result = ReformatModule.SimpleBreaks(words, L: 5, last: false);

            Assert.Equal(4, result);
            Assert.Equal(1, words[0].NextLine);
            Assert.Null(words[1].NextLine);
        }
    }
}

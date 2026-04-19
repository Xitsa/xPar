using xParLib;

namespace xParTests;

public class LineReaderTests
{
    // LineReader.ReadLines теперь static — вызываем напрямую

    // Charset'ы по умолчанию для тестов
    private static Charset EmptyCharset => Charset.Parse("");
    private static Charset DefaultQuoteChars => Charset.Parse("> ");
    private static Charset DefaultWhiteChars => Charset.Parse(" \f\n\r\t\v");

    #region Базовые тесты

    [Fact]
    public void ReadLines_EmptyInput_ReturnsEmptyResult()
    {
        // Act
        var result = LineReader.ReadLines(
            Array.Empty<string>(), 0,
            EmptyCharset, DefaultQuoteChars, DefaultWhiteChars,
            tab: 1, invis: false, quote: false);

        // Assert
        Assert.Empty(result.Segments);
        Assert.Equal(0, result.NextIndex);
        Assert.True(result.IsEof);
    }

    [Fact]
    public void ReadLines_NormalLines_ReturnsAllSegments()
    {
        // Arrange
        var lines = new[] { "Hello", "World", "Test" };

        // Act
        var result = LineReader.ReadLines(
            lines, 0,
            EmptyCharset, DefaultQuoteChars, DefaultWhiteChars,
            tab: 1, invis: false, quote: false);

        // Assert
        Assert.Equal(3, result.Segments.Length);
        Assert.Equal("Hello", result.Segments[0].Line);
        Assert.Equal("World", result.Segments[1].Line);
        Assert.Equal("Test", result.Segments[2].Line);
        Assert.Equal(3, result.NextIndex);
        Assert.True(result.IsEof);
    }

    #endregion

    #region Protected line

    [Fact]
    public void ReadLines_ProtectedLine_StopsBeforeIt()
    {
        // Arrange — '#' как защитный символ
        var protectChars = Charset.Parse("#");
        var lines = new[] { "Hello", "World", "# Protected", "More" };

        // Act
        var result = LineReader.ReadLines(
            lines, 0,
            protectChars, EmptyCharset, DefaultWhiteChars,
            tab: 1, invis: false, quote: false);

        // Assert
        Assert.Equal(2, result.Segments.Length);
        Assert.Equal("Hello", result.Segments[0].Line);
        Assert.Equal("World", result.Segments[1].Line);
        Assert.Equal(2, result.NextIndex); // Указывает на "# Protected"
        Assert.False(result.IsEof);
    }

    [Fact]
    public void ReadLines_FirstLineProtected_ReturnsEmpty()
    {
        // Arrange
        var protectChars = Charset.Parse("#");
        var lines = new[] { "# Protected", "Hello" };

        // Act
        var result = LineReader.ReadLines(
            lines, 0,
            protectChars, EmptyCharset, DefaultWhiteChars,
            tab: 1, invis: false, quote: false);

        // Assert
        Assert.Empty(result.Segments);
        Assert.Equal(0, result.NextIndex);
        Assert.False(result.IsEof);
    }

    #endregion

    #region Blank line

    [Fact]
    public void ReadLines_BlankLine_StopsBeforeIt()
    {
        // Arrange
        var lines = new[] { "Hello", "World", "", "More" };

        // Act
        var result = LineReader.ReadLines(
            lines, 0,
            EmptyCharset, EmptyCharset, DefaultWhiteChars,
            tab: 1, invis: false, quote: false);

        // Assert
        Assert.Equal(2, result.Segments.Length);
        Assert.Equal("Hello", result.Segments[0].Line);
        Assert.Equal("World", result.Segments[1].Line);
        Assert.Equal(2, result.NextIndex); // Указывает на пустую строку
        Assert.False(result.IsEof);
    }

    [Fact]
    public void ReadLines_BlankLineWithSpaces_StopsBeforeIt()
    {
        // Arrange
        var lines = new[] { "Hello", "   ", "More" };

        // Act
        var result = LineReader.ReadLines(
            lines, 0,
            EmptyCharset, EmptyCharset, DefaultWhiteChars,
            tab: 1, invis: false, quote: false);

        // Assert
        Assert.Single(result.Segments);
        Assert.Equal("Hello", result.Segments[0].Line);
        Assert.Equal(1, result.NextIndex); // Указывает на "   "
        Assert.False(result.IsEof);
    }

    #endregion

    #region Tab expansion

    [Fact]
    public void ReadLines_TabExpansion_ExpandsToSpaces()
    {
        // Arrange
        var lines = new[] { "a\tb" };

        // Act
        var result = LineReader.ReadLines(
            lines, 0,
            EmptyCharset, EmptyCharset, DefaultWhiteChars,
            tab: 4, invis: false, quote: false);

        // Assert
        Assert.Single(result.Segments);
        Assert.Equal("a   b", result.Segments[0].Line); // a + 3 пробела (4 - 1)
    }

    [Fact]
    public void ReadLines_TabAtStart_ExpandsToFullTab()
    {
        // Arrange
        var lines = new[] { "\thello" };

        // Act
        var result = LineReader.ReadLines(
            lines, 0,
            EmptyCharset, EmptyCharset, DefaultWhiteChars,
            tab: 8, invis: false, quote: false);

        // Assert
        Assert.Single(result.Segments);
        Assert.Equal("        hello", result.Segments[0].Line); // 8 пробелов
    }

    [Fact]
    public void ReadLines_MultipleTabs_ExpandsEach()
    {
        // Arrange
        var lines = new[] { "a\t\tb" };

        // Act
        var result = LineReader.ReadLines(
            lines, 0,
            EmptyCharset, EmptyCharset, DefaultWhiteChars,
            tab: 4, invis: false, quote: false);

        // Assert
        Assert.Single(result.Segments);
        // a (width 1) + 3 spaces → pos 4, затем tab → ещё 4 spaces → pos 8, затем b
        Assert.Equal("a       b", result.Segments[0].Line);
    }

    #endregion

    #region Whitespace замена

    [Fact]
    public void ReadLines_WhitespaceReplaced_Spaces()
    {
        // Arrange — \f (formfeed) — whitespace
        var lines = new[] { "a\fb" };

        // Act
        var result = LineReader.ReadLines(
            lines, 0,
            EmptyCharset, EmptyCharset, DefaultWhiteChars,
            tab: 1, invis: false, quote: false);

        // Assert
        Assert.Single(result.Segments);
        Assert.Equal("a b", result.Segments[0].Line);
    }

    [Fact]
    public void ReadLines_NulSkipped()
    {
        // Arrange
        var lines = new[] { "a\0b" };

        // Act
        var result = LineReader.ReadLines(
            lines, 0,
            EmptyCharset, EmptyCharset, DefaultWhiteChars,
            tab: 1, invis: false, quote: false);

        // Assert
        Assert.Single(result.Segments);
        Assert.Equal("ab", result.Segments[0].Line);
    }

    #endregion

    #region StartIndex

    [Fact]
    public void ReadLines_StartIndex_SkipsInitialLines()
    {
        // Arrange
        var lines = new[] { "Skip1", "Skip2", "Hello", "World" };

        // Act
        var result = LineReader.ReadLines(
            lines, 2,
            EmptyCharset, EmptyCharset, DefaultWhiteChars,
            tab: 1, invis: false, quote: false);

        // Assert
        Assert.Equal(2, result.Segments.Length);
        Assert.Equal("Hello", result.Segments[0].Line);
        Assert.Equal("World", result.Segments[1].Line);
    }

    #endregion

    #region Quote-логика

    [Fact]
    public void ReadLines_Quote_InsertsVacantLineBetweenLevels()
    {
        // Arrange
        var quoteChars = Charset.Parse(">");
        var lines = new[] { "> Level 1", ">> Level 2", "> Back to 1" };

        // Act
        var result = LineReader.ReadLines(
            lines, 0,
            EmptyCharset, quoteChars, DefaultWhiteChars,
            tab: 1, invis: false, quote: true);

        // Assert
        // "> Level 1", vacant ">", ">> Level 2", vacant ">", "> Back to 1"
        Assert.Equal(5, result.Segments.Length);
        Assert.Equal("> Level 1", result.Segments[0].Line);
        Assert.Equal(">", result.Segments[1].Line);
        Assert.Equal(LineFlags.Inserted, result.Segments[1].Prop.Flags);
        Assert.Equal(">> Level 2", result.Segments[2].Line);
        Assert.Equal(">", result.Segments[3].Line);
        Assert.Equal(LineFlags.Inserted, result.Segments[3].Prop.Flags);
        Assert.Equal("> Back to 1", result.Segments[4].Line);
    }

    [Fact]
    public void ReadLines_Quote_InsertsVacantLineBetweenLevelsFromDocs()
    {
        // Arrange
        var quoteChars = Charset.Parse(" >");
        var lines = new[] {
"        Joe Public writes:",
"        > Jane Doe writes:",
"        > >",
"        > >",
"        > > I can't find the source for uncompress.",
"        > Oh no, not again!!!",
"        >",
"        >",
"        > Isn't there a FAQ for this?",
"        >",
"        >",
"        That wasn't very helpful, Joe. Jane,",
"        just make a link from uncompress to compress."
        };

        // Act
        var result = LineReader.ReadLines(
            lines, 0,
            EmptyCharset, quoteChars, DefaultWhiteChars,
            tab: 1, invis: false, quote: true);

        // Assert
        Assert.Equal(15, result.Segments.Length);

        Assert.Equal("        Joe Public writes:"                              , result.Segments[0].Line);
        Assert.Equal(""                                                        , result.Segments[1].Line);
        Assert.Equal(LineFlags.Inserted, result.Segments[1].Prop.Flags);
        Assert.Equal("        > Jane Doe writes:"                              , result.Segments[2].Line);
        Assert.Equal("        >"                                               , result.Segments[3].Line);
        Assert.Equal("        >"                                               , result.Segments[4].Line);
        Assert.Equal("        > > I can't find the source for uncompress."     , result.Segments[5].Line);
        Assert.Equal("        >"                                               , result.Segments[6].Line);
        Assert.Equal(LineFlags.Inserted, result.Segments[6].Prop.Flags);
        Assert.Equal("        > Oh no, not again!!!"                           , result.Segments[7].Line);
        Assert.Equal("        >"                                               , result.Segments[8].Line);
        Assert.Equal("        >"                                               , result.Segments[9].Line);
        Assert.Equal("        > Isn't there a FAQ for this?"                   , result.Segments[10].Line);
        Assert.Equal("        >"                                               , result.Segments[11].Line);
        Assert.Equal(""                                                        , result.Segments[12].Line);
        Assert.Equal("        That wasn't very helpful, Joe. Jane,"            , result.Segments[13].Line);
        Assert.Equal("        just make a link from uncompress to compress."   , result.Segments[14].Line);
    }

    [Fact]
    public void ReadLines_Quote_SameLevel_NoInsertion()
    {
        // Arrange
        var quoteChars = Charset.Parse(">");
        var lines = new[] { "> One", "> Two", "> Three" };

        // Act
        var result = LineReader.ReadLines(
            lines, 0,
            EmptyCharset, quoteChars, DefaultWhiteChars,
            tab: 1, invis: false, quote: true);

        // Assert
        Assert.Equal(3, result.Segments.Length);
        Assert.All(result.Segments, s => Assert.Equal(LineFlags.None, s.Prop.Flags));
    }

    #endregion

    #region LineProp проверка

    [Fact]
    public void ReadLines_NormalLines_HaveDefaultProps()
    {
        // Arrange
        var lines = new[] { "Hello", "World" };

        // Act
        var result = LineReader.ReadLines(
            lines, 0,
            EmptyCharset, EmptyCharset, DefaultWhiteChars,
            tab: 1, invis: false, quote: false);

        // Assert
        Assert.All(result.Segments, s =>
        {
            Assert.Equal(0, s.Prop.P);
            Assert.Equal(0, s.Prop.S);
            Assert.Equal(LineFlags.None, s.Prop.Flags);
            Assert.Equal("", s.Prop.Rc);
        });
    }

    #endregion
}

// ============================================================
// Тесты для Compresuflen
// ============================================================

public class CompresuflenTests
{
    private static Charset EmptyBodyChars => Charset.Parse("");
    private static Charset AlphaBodyChars => Charset.Parse("_A_a");
    private static Charset AlphaPointBodyChars => Charset.Parse("._A_a");

    // Helper: преобразует string[] в LineSegment[] с пустыми флагами
    private static LineSegment[] ToSegments(params string[] lines)
    {
        return lines.Select(l => new LineSegment(l, new LineProp())).ToArray();
    }

    #region Базовые тесты

    [Fact]
    public void Compresuflen_SingleLine_PrefixIsWholeLine()
    {
        // Одна строка: при body=0 весь префикс — до первого body-символа (нет body → вся строка)
        var segments = ToSegments("abc");

        var result = LineReader.Compresuflen(segments, 0, 0, EmptyBodyChars, body: false, minPrefix: 0, minSuffix: 0);

        Assert.Equal(3, result.Prefix); // "abc" — 3 графемы
        Assert.Equal(0, result.Suffix);
    }

    [Fact]
    public void Compresuflen_TwoIdenticalLines_PrefixIsWholeLine()
    {
        var segments = ToSegments("abc", "abc");

        var result = LineReader.Compresuflen(segments, 0, 1, EmptyBodyChars, body: false, minPrefix: 0, minSuffix: 0);

        Assert.Equal(3, result.Prefix);
        Assert.Equal(0, result.Suffix);
    }

    [Fact]
    public void Compresuflen_TwoLinesWithCommonPrefix()
    {
        var segments = ToSegments("abcdef", "abcxyz");

        var result = LineReader.Compresuflen(segments, 0, 1, EmptyBodyChars, body: false, minPrefix: 0, minSuffix: 0);

        Assert.Equal(3, result.Prefix); // "abc"
    }

    [Fact]
    public void Compresuflen_TwoLinesWithCommonSuffix()
    {
        var segments = ToSegments("abcmno", "xyzmno");

        var result = LineReader.Compresuflen(segments, 0, 1, EmptyBodyChars, body: false, minPrefix: 0, minSuffix: 0);

        Assert.Equal(3, result.Suffix); // "mno"
    }

    #endregion

    #region Тесты с body=0

    [Fact]
    public void Compresuflen_Body0_StopsBeforeBodyChars()
    {
        // body=0: префикс останавливается перед первым body-символом
        // ". abc" и ". def" — общий префикс ". " (2 графемы), потом 'a' и 'd' — не body,
        // но '.' — body, так что префикс до '.' → 2
        var segments = ToSegments("> text1 .", "> text2 .");

        // BodyChars = буквы; префикс "> " — 2 графемы
        var result = LineReader.Compresuflen(segments, 0, 1, AlphaBodyChars, body: false, minPrefix: 0, minSuffix: 0);

        Assert.Equal(2, result.Prefix); // "> "
    }

    [Fact]
    public void Compresuflen_Body0_SuffixStopsBeforeTrailingBody()
    {
        // body=0: суффикс не включает trailing body-символы
        var segments = ToSegments("abc !", "xyz !");
        // " !" — суффикс, '!' не body, пробел не body → " !" = 2 графемы
        var result = LineReader.Compresuflen(segments, 0, 1, AlphaBodyChars, body: false, minPrefix: 0, minSuffix: 0);

        Assert.Equal(2, result.Suffix);
    }

    #endregion

    #region Тесты с body=1

    [Fact]
    public void Compresuflen_Body1_PrefixUpToLastNonSpaceNonBody()
    {
        // body=1: префикс до последнего non-space non-body символа.
        // "abc def" и "abc xyz" — общий префикс "abc ", но все символы — body (буквы),
        // нет non-body → префикс = 0.
        var segments = ToSegments("abc def", "abc xyz");

        var result = LineReader.Compresuflen(segments, 0, 1, AlphaBodyChars, body: true, minPrefix: 0, minSuffix: 0);

        Assert.Equal(0, result.Prefix);
    }

    [Fact]
    public void Compresuflen_Body1_PrefixWithNonBody()
    {
        // body=1: "ab! def" и "ab! xyz" — общий префикс "ab! " (4 графемы),
        // '! ' — non-body, но '!' — последний non-space non-body →
        // корректировка не двигает end (пробел пропускаем, '!' — non-body → break)
        // prefix = 4
        var segments = ToSegments("ab! def", "ab! xyz");

        var result = LineReader.Compresuflen(segments, 0, 1, AlphaBodyChars, body: true, minPrefix: 0, minSuffix: 0);

        Assert.Equal(4, result.Prefix); // "ab! "
    }

    [Fact]
    public void Compresuflen_Body1_SuffixAdjustsForSpace()
    {
        // body=1: "abc  " и "abc " — общий префикс после A3 = "abc " (4 графемы),
        // но body=1 корректировка: 'c' — body → префикс сужается до 0.
        // Суффикс: после префикса 0, baseGraphemes = ['a','b','c',' ',' '], bKnownEnd = 5
        // bStart = bKnownStart = 0; для второй строки g=['a','b','c',' '], p1=5, p2=4
        // g[3]=' ' vs baseGraphemes[4]=' ' → равны → p1=4,p2=3
        // g[2]='c' vs baseGraphemes[3]=' ' → не равны → bStart=4
        // body=1: bStart=4, knownEnd=5, baseGraphemes[4]=' ' → bStart=5, затем откат на 1 → bStart=4
        // suffix = 5 - 4 = 1
        var segments = ToSegments("abc  ", "abc ");

        var result = LineReader.Compresuflen(segments, 0, 1, AlphaBodyChars, body: true, minPrefix: 0, minSuffix: 0);

        Assert.Equal(0, result.Prefix);
        Assert.Equal(1, result.Suffix);
    }


    [Fact]
    public void Compresuflen_Body1_PrefixSuffixCare()
    {
        // body=1: три строки с общим префиксом "        amc> " (13 графем),
        // но body=1 корректировка: буквы — body, так что префикс включает только
        // не-body символы после последнего body → "        amc> " = 13
        var segments = ToSegments(
"        amc> Par still pays attention to body characters.",
"        amc> Par should not mistake \"Par\" for part of the prefix.",
"        amc> Par should not mistake \".\" for a suffix."
        );

        var result = LineReader.Compresuflen(segments, 0, 2, AlphaPointBodyChars, body: true, minPrefix: 0, minSuffix: 0);

        Assert.Equal(13, result.Prefix);
        Assert.Equal(0, result.Suffix);
    }

    #endregion

    #region Тесты с minPrefix/minSuffix > 0

    [Fact]
    public void Compresuflen_MinPrefix_SkipsKnownPrefix()
    {
        // minPrefix=2: начинаем сравнение с 3-го графема
        var segments = ToSegments("abXYZ", "abXUV");

        var result = LineReader.Compresuflen(segments, 0, 1, EmptyBodyChars, body: false, minPrefix: 2, minSuffix: 0);

        Assert.Equal(3, result.Prefix); // "abX" — общий префикс после 2 = 3 графемы
    }

    [Fact]
    public void Compresuflen_MinSuffix_SkipsKnownSuffix()
    {
        // minSuffix=1: начинаем сравнение, отступив 1 графем от конца
        var segments = ToSegments("XYab", "ZWab");
        // Общий суффикс: "ab" = 2 графемы
        var result = LineReader.Compresuflen(segments, 0, 1, EmptyBodyChars, body: false, minPrefix: 0, minSuffix: 1);

        Assert.Equal(2, result.Suffix); // "ab"
    }

    #endregion

    #region Unicode-тесты

    [Fact]
    public void Compresuflen_WithEmoji_CorrectGraphemeCount()
    {
        // Эмодзи — каждый считается как одна графема
        var segments = ToSegments("👋🌍abc", "👋🌍def");

        var result = LineReader.Compresuflen(segments, 0, 1, EmptyBodyChars, body: false, minPrefix: 0, minSuffix: 0);

        // "👋🌍abc" vs "👋🌍def" → общий префикс "👋🌍" = 2 графемы
        Assert.Equal(2, result.Prefix);
    }

    [Fact]
    public void Compresuflen_WithCombiningCharacters_CorrectCount()
    {
        // "é" как e + combining acute (U+0301) — одна графема
        string eAcute1 = "e\u0301"; // é combining
        string eAcute2 = "e\u0301"; // é combining
        var segments = ToSegments(eAcute1 + "abc", eAcute2 + "xyz");

        var result = LineReader.Compresuflen(segments, 0, 1, EmptyBodyChars, body: false, minPrefix: 0, minSuffix: 0);

        Assert.Equal(1, result.Prefix); // одна графема "é"
    }

    #endregion

    #region StartIndex тесты

    [Fact]
    public void Compresuflen_StartIndex_SkipsInitialLines()
    {
        var segments = ToSegments("different", "abcXY", "abcXZ");

        // startIndex=1, endIndex=2 → обрабатываем только "abcXY" и "abcXZ"
        var result = LineReader.Compresuflen(segments, 1, 2, EmptyBodyChars, body: false, minPrefix: 0, minSuffix: 0);

        Assert.Equal(4, result.Prefix); // "abcX"
    }

    #endregion
}

// ============================================================
// Тесты для Delimit
// ============================================================

public class DelimitTests
{
    private static Charset EmptyBodyChars => Charset.Parse("");
    private static Charset AlphaBodyChars => Charset.Parse("_A_a");
    private static Charset AlphaPointBodyChars => Charset.Parse("._A_a");

    // Helper: преобразует string[] в LineSegment[] с пустыми флагами
    private static LineSegment[] ToSegments(params string[] lines)
    {
        return lines.Select(l => new LineSegment(l, new LineProp())).ToArray();
    }

    #region Базовые тесты

    [Fact]
    public void Delimit_EmptyRange_NoChanges()
    {
        var segments = ToSegments("abc");

        LineReader.Delimit(segments, 1, 0, EmptyBodyChars, repeat: 0, body: false, div: false, minPrefix: 0, minSuffix: 0);

        Assert.Equal(LineFlags.None, segments[0].Prop.Flags);
    }

    [Fact]
    public void Delimit_SingleSegment_FirstFlagSet()
    {
        var segments = ToSegments("hello world");

        LineReader.Delimit(segments, 0, 0, EmptyBodyChars, repeat: 0, body: false, div: false, minPrefix: 2, minSuffix: 3);

        var prop = segments[0].Prop;
        Assert.Equal(LineFlags.First, prop.Flags);
        Assert.Equal(2, prop.P);
        Assert.Equal(3, prop.S);
    }

    [Fact]
    public void Delimit_TwoIdenticalSegments_FirstAndBodiless()
    {
        // Две одинаковые строки без body → обе bodiless (строки из одинаковых символов)
        var segments = ToSegments("abc", "abc");

        LineReader.Delimit(segments, 0, 1, EmptyBodyChars, repeat: 0, body: false, div: false, minPrefix: 0, minSuffix: 0);

        // Обе строки "abc" — одинаковые символы → bodiless с rc='a'
        Assert.True(IsBodiless(segments[0].Prop));
        Assert.True(IsBodiless(segments[1].Prop));
    }

    #endregion

    #region Тесты bodiless

    [Fact]
    public void Delimit_SpaceLine_Bodiless()
    {
        // Строка из пробелов между двумя строками
        var segments = ToSegments("hello", "     ", "world");

        LineReader.Delimit(segments, 0, 2, EmptyBodyChars, repeat: 0, body: false, div: false, minPrefix: 0, minSuffix: 0);

        // Средняя строка — bodiless с rc=" "
        Assert.True(IsBodiless(segments[1].Prop));
        Assert.Equal(" ", segments[1].Prop.Rc);
    }

    [Fact]
    public void Delimit_RepeatedChars_Bodiless()
    {
        // Строка из повторяющихся символов "---" (repeat=3) → bodiless
        var segments = ToSegments("hello", "---", "world");

        LineReader.Delimit(segments, 0, 2, EmptyBodyChars, repeat: 3, body: false, div: false, minPrefix: 0, minSuffix: 0);

        Assert.True(IsBodiless(segments[1].Prop));
        Assert.Equal("-", segments[1].Prop.Rc);
    }

    [Fact]
    public void Delimit_TooShortRepeat_NotBodiless()
    {
        // Строка из "--" (repeat=3) → НЕ bodiless (слишком короткая)
        var segments = ToSegments("hello", "--", "world");

        LineReader.Delimit(segments, 0, 2, EmptyBodyChars, repeat: 3, body: false, div: false, minPrefix: 0, minSuffix: 0);

        Assert.False(IsBodiless(segments[1].Prop));
    }

    [Fact]
    public void Delimit_InsertedWithNonSpace_NotBodiless()
    {
        // Вставленная строка (Inserted) с rc != " " → НЕ bodiless
        var segs = new[]
        {
            new LineSegment("hello", new LineProp()),
            new LineSegment("---", new LineProp(flags: LineFlags.Inserted)),
            new LineSegment("world", new LineProp()),
        };

        LineReader.Delimit(segs, 0, 2, EmptyBodyChars, repeat: 0, body: false, div: false, minPrefix: 0, minSuffix: 0);

        Assert.False(IsBodiless(segs[1].Prop));
    }

    #endregion

    #region Тесты рекурсии

    [Fact]
    public void Delimit_BodilessInMiddle_RecursiveSubblocks()
    {
        // "aaa", "---", "bbb" → все три строки bodiless (одинаковые символы),
        // но "---" тоже bodiless, и "aaa"/"bbb" — тоже (все символы одинаковы).
        // При рекурсии подблоки получают First, но Bodiless сохраняется.
        var segments = ToSegments("aaa", "---", "bbb");

        LineReader.Delimit(segments, 0, 2, EmptyBodyChars, repeat: 3, body: false, div: false, minPrefix: 0, minSuffix: 0);

        Assert.True(IsBodiless(segments[0].Prop)); // "aaa" — все одинаковые
        Assert.True(IsBodiless(segments[1].Prop)); // "---" — все одинаковые
        Assert.True(IsBodiless(segments[2].Prop)); // "bbb" — все одинаковые
        // Все bodiless → нет рекурсии → First не ставится
    }

    [Fact]
    public void Delimit_MultipleBodilessLines_EachMarked()
    {
        // "aaa", "---", "bbb", "===" → все bodiless (одинаковые символы)
        var segments = ToSegments("aaa", "---", "bbb", "===");

        LineReader.Delimit(segments, 0, 3, EmptyBodyChars, repeat: 3, body: false, div: false, minPrefix: 0, minSuffix: 0);

        Assert.True(IsBodiless(segments[0].Prop));
        Assert.True(IsBodiless(segments[1].Prop));
        Assert.True(IsBodiless(segments[2].Prop));
        Assert.True(IsBodiless(segments[3].Prop));
    }

    #endregion

    #region Тесты div

    [Fact]
    public void Delimit_NoDiv_OnlyFirstGetsFirstFlag()
    {
        // div=false, нет bodiless → только первая строка получает First
        var segments = ToSegments("hello world", "foo bar baz");

        LineReader.Delimit(segments, 0, 1, EmptyBodyChars, repeat: 0, body: false, div: false, minPrefix: 0, minSuffix: 0);

        Assert.Equal(LineFlags.First, segments[0].Prop.Flags);
        Assert.Equal(LineFlags.None, segments[1].Prop.Flags & LineFlags.First);
    }

    [Fact]
    public void Delimit_WithDiv_SameStatusGetsFirst()
    {
        // div=true: строки с одинаковым статусом пробела на позиции minPrefix
        // " abc" (пробел на 0) и " def" (пробел на 0) → обе First
        var segments = ToSegments(" abc", " def");

        LineReader.Delimit(segments, 0, 1, EmptyBodyChars, repeat: 0, body: false, div: true, minPrefix: 0, minSuffix: 0);

        Assert.Equal(LineFlags.First, segments[0].Prop.Flags);
        Assert.Equal(LineFlags.First, segments[1].Prop.Flags);
    }

    [Fact]
    public void Delimit_WithDiv_DifferentStatus()
    {
        // div=true: разные статусы → только первая и совпадающие
        // "abc" (нет пробела на 0), " def" (пробел на 0) → только первая First
        var segments = ToSegments("abc", " def");

        LineReader.Delimit(segments, 0, 1, EmptyBodyChars, repeat: 0, body: false, div: true, minPrefix: 0, minSuffix: 0);

        Assert.Equal(LineFlags.First, segments[0].Prop.Flags);
        Assert.Equal(LineFlags.None, segments[1].Prop.Flags & LineFlags.First);
    }

    #endregion

    #region Тесты body=1

    [Fact]
    public void Delimit_Body1_PrefixSuffixWithBody()
    {
        // body=1: префикс/суффикс с учётом body-символов
        // "> text1 ." и "> text2 ." → общий префикс "> text" (6),
        // body=1 корректировка: все буквы — body, отступаем до "> " → P=2
        // суффикс: " ." → S=2
        var segments = ToSegments("> text1 .", "> text2 .");

        LineReader.Delimit(segments, 0, 1, AlphaBodyChars, repeat: 0, body: true, div: false, minPrefix: 0, minSuffix: 0);

        Assert.Equal(2, segments[0].Prop.P);
        Assert.Equal(2, segments[0].Prop.S);
    }

    [Fact]
    public void Delimit_Body1_CuriousPrefixSuffixWithBody()
    {
        var segments = ToSegments(
"        amc> Par still pays attention to body characters.",
"        amc> Par should not mistake \"Par\" for part of the prefix.",
"        amc> Par should not mistake \".\" for a suffix."
        );

        LineReader.Delimit(segments, 0, 2, AlphaPointBodyChars, repeat: 0, body: true, div: false, minPrefix: 0, minSuffix: 0);

        Assert.Equal(13, segments[0].Prop.P);
        Assert.Equal(0, segments[0].Prop.S);
        Assert.Equal(LineFlags.First, segments[0].Prop.Flags);
        Assert.Equal(13, segments[1].Prop.P);
        Assert.Equal(0, segments[1].Prop.S);
        Assert.Equal(13, segments[2].Prop.P);
        Assert.Equal(0, segments[2].Prop.S);
    }

    #endregion

    // Helper для проверки bodiless
    private static bool IsBodiless(LineProp prop)
    {
        return (prop.Flags & LineFlags.Bodiless) != 0;
    }
}

// ============================================================
// Тесты для MarkSuperf
// ============================================================

public class MarkSuperfTests
{
    // Helper: преобразует string[] в LineSegment[] с пустыми флагами
    private static LineSegment[] ToSegments(params string[] lines)
    {
        return lines.Select(l => new LineSegment(l, new LineProp())).ToArray();
    }

    #region Базовые тесты

    [Fact]
    public void MarkSuperf_EmptyRange_NoChanges()
    {
        var segments = ToSegments("abc");

        LineReader.MarkSuperf(segments, 1, 0);

        Assert.Equal(LineFlags.None, segments[0].Prop.Flags);
    }

    [Fact]
    public void MarkSuperf_SingleVacant_SuperfTrue()
    {
        // Один vacant сегмент (bodiless + Rc = " ")
        var segments = new[]
        {
            new LineSegment("   ", new LineProp(flags: LineFlags.Bodiless, rc: " ")),
        };

        LineReader.MarkSuperf(segments, 0, 0);

        Assert.Equal(LineFlags.Superf, segments[0].Prop.Flags & LineFlags.Superf);
    }

    [Fact]
    public void MarkSuperf_SingleNonVacant_NoSuperf()
    {
        var segments = ToSegments("hello");

        LineReader.MarkSuperf(segments, 0, 0);

        Assert.Equal(LineFlags.None, segments[0].Prop.Flags & LineFlags.Superf);
    }

    #endregion

    #region Vacant в начале/конце

    [Fact]
    public void MarkSuperf_VacantAtStart_SuperfTrue()
    {
        // Vacant в начале → все избыточные
        var segments = new[]
        {
            new LineSegment("  ", new LineProp(flags: LineFlags.Bodiless, rc: " ")),
            new LineSegment("hello", new LineProp()),
        };

        LineReader.MarkSuperf(segments, 0, 1);

        Assert.Equal(LineFlags.Superf, segments[0].Prop.Flags & LineFlags.Superf);
    }

    [Fact]
    public void MarkSuperf_VacantAtEnd_SuperfTrue()
    {
        // Vacant в конце → все избыточные
        var segments = new[]
        {
            new LineSegment("hello", new LineProp()),
            new LineSegment("  ", new LineProp(flags: LineFlags.Bodiless, rc: " ")),
        };

        LineReader.MarkSuperf(segments, 0, 1);

        Assert.Equal(LineFlags.Superf, segments[1].Prop.Flags & LineFlags.Superf);
    }

    [Fact]
    public void MarkSuperf_MultipleVacantAtStart_AllSuperf()
    {
        // Несколько contiguous vacant в начале → все Superf
        var segments = new[]
        {
            new LineSegment(" ", new LineProp(flags: LineFlags.Bodiless, rc: " ")),
            new LineSegment("  ", new LineProp(flags: LineFlags.Bodiless, rc: " ")),
            new LineSegment("   ", new LineProp(flags: LineFlags.Bodiless, rc: " ")),
            new LineSegment("hello", new LineProp()),
        };

        LineReader.MarkSuperf(segments, 0, 3);

        Assert.Equal(LineFlags.Superf, segments[0].Prop.Flags & LineFlags.Superf);
        Assert.Equal(LineFlags.Superf, segments[1].Prop.Flags & LineFlags.Superf);
        Assert.Equal(LineFlags.Superf, segments[2].Prop.Flags & LineFlags.Superf);
    }

    #endregion

    #region Vacant между non-vacant

    [Fact]
    public void MarkSuperf_SingleVacantBetweenNonVacant_NotSuperf()
    {
        // Одна vacant между двумя non-vacant → Superf = false (единственная в группе)
        var segments = new[]
        {
            new LineSegment("hello", new LineProp()),
            new LineSegment(" ", new LineProp(flags: LineFlags.Bodiless, rc: " ")),
            new LineSegment("world", new LineProp()),
        };

        LineReader.MarkSuperf(segments, 0, 2);

        Assert.Equal(LineFlags.None, segments[1].Prop.Flags & LineFlags.Superf);
    }

    [Fact]
    public void MarkSuperf_TwoVacantBetweenNonVacant_BestNotSuperf()
    {
        // Две vacant между non-vacant: лучшая (меньше non-space) → не Superf
        var segments = new[]
        {
            new LineSegment("hello", new LineProp()),
            new LineSegment(" x ", new LineProp(flags: LineFlags.Bodiless, rc: " ")), // 1 non-space
            new LineSegment("  x  ", new LineProp(flags: LineFlags.Bodiless, rc: " ")), // 1 non-space
            new LineSegment("world", new LineProp()),
        };

        LineReader.MarkSuperf(segments, 0, 3);

        // Первая vacant (tie-break: первая выбирается) → не Superf
        Assert.Equal(LineFlags.None, segments[1].Prop.Flags & LineFlags.Superf);
        Assert.Equal(LineFlags.Superf, segments[2].Prop.Flags & LineFlags.Superf);
    }

    #endregion

    #region Несколько групп

    [Fact]
    public void MarkSuperf_MultipleGroups_CorrectMarking()
    {
        // non-vacant, vacant, vacant, non-vacant, vacant, non-vacant
        // Группа 1: vacant[1],vacant[2] → лучшая не Superf
        // Группа 2: vacant[4] → одиночная между non-vacant → не Superf
        var segments = new[]
        {
            new LineSegment("hello", new LineProp()),
            new LineSegment(" ", new LineProp(flags: LineFlags.Bodiless, rc: " ")),     // 0 non-space
            new LineSegment(" x ", new LineProp(flags: LineFlags.Bodiless, rc: " ")),   // 1 non-space
            new LineSegment("world", new LineProp()),
            new LineSegment("  ", new LineProp(flags: LineFlags.Bodiless, rc: " ")),    // 0 non-space
            new LineSegment("end", new LineProp()),
        };

        LineReader.MarkSuperf(segments, 0, 5);

        Assert.Equal(LineFlags.None, segments[1].Prop.Flags & LineFlags.Superf);   // лучшая в группе 1
        Assert.Equal(LineFlags.Superf, segments[2].Prop.Flags & LineFlags.Superf);
        Assert.Equal(LineFlags.None, segments[4].Prop.Flags & LineFlags.Superf);   // одиночная
    }

    #endregion

    #region Non-vacant не затрагиваются

    [Fact]
    public void MarkSuperf_BodilessNotVacant_NotAffected()
    {
        // Bodiless но rc != " " → не vacant → Superf не затрагивается
        var segments = new[]
        {
            new LineSegment("---", new LineProp(flags: LineFlags.Bodiless, rc: "-")),
            new LineSegment("hello", new LineProp()),
        };

        LineReader.MarkSuperf(segments, 0, 1);

        Assert.Equal(LineFlags.None, segments[0].Prop.Flags & LineFlags.Superf);
    }

    [Fact]
    public void MarkSuperf_NormalLines_NotAffected()
    {
        // Обычные строки без Bodiless → Superf не затрагивается
        var segments = ToSegments("hello", "world", "test");

        LineReader.MarkSuperf(segments, 0, 2);

        Assert.All(segments, s => Assert.Equal(LineFlags.None, s.Prop.Flags & LineFlags.Superf));
    }

    #endregion
}

// ============================================================
// Тесты для SetAffixes
// ============================================================

public class SetAffixesTests
{
    private static Charset EmptyBodyChars => Charset.Parse("");
    private static Charset DefaultQuoteChars => Charset.Parse("> ");
    private static Charset AlphaBodyChars => Charset.Parse("_A_a");

    // Helper: преобразует string[] в LineSegment[] с заданными P и S
    private static LineSegment[] ToSegmentsWithProps(int p, int s, params string[] lines)
    {
        return lines.Select(l => new LineSegment(l, new LineProp(p: p, s: s))).ToArray();
    }

    #region Базовые тесты

    [Fact]
    public void SetAffixes_PrefixSuffixProvided_ReturnsAsIs()
    {
        // prefix/suffix заданы → возвращаем их, fallback не считается
        var segments = ToSegmentsWithProps(3, 2, "hello");

        var result = LineReader.SetAffixes(segments, 0, 0, EmptyBodyChars, EmptyBodyChars,
            hang: 0, body: false, quote: false, prefix: 5, suffix: 7);

        Assert.Equal(5, result.Prefix);
        Assert.Equal(7, result.Suffix);
    }

    [Fact]
    public void SetAffixes_SingleLine_UnsetPrefixSuffix_UsesFallback()
    {
        // Одна строка, prefix/suffix = null → fallback из Prop
        var segments = ToSegmentsWithProps(3, 2, "hello");

        var result = LineReader.SetAffixes(segments, 0, 0, EmptyBodyChars, EmptyBodyChars,
            hang: 0, body: false, quote: false, prefix: null, suffix: null);

        Assert.Equal(3, result.Prefix);
        Assert.Equal(2, result.Suffix);
        Assert.Equal(3, result.AugmentedFallbackPre);
        Assert.Equal(2, result.FallbackSuf);
    }

    [Fact]
    public void SetAffixes_TwoLines_UnsetPrefixSuffix_UsesCompresuflen()
    {
        // Две строки, numin=2 > hang+1=1 → compresuflen
        var segments = ToSegmentsWithProps(0, 0, "abcdef", "abcxyz");

        var result = LineReader.SetAffixes(segments, 0, 1, EmptyBodyChars, EmptyBodyChars,
            hang: 0, body: false, quote: false, prefix: null, suffix: null);

        Assert.Equal(3, result.Prefix);  // "abc"
        Assert.Equal(0, result.Suffix);  // нет общего суффикса
    }

    #endregion

    #region Тесты с hang

    [Fact]
    public void SetAffixes_Hang_SkipsFirstLines()
    {
        // Три строки, hang=1 → compresuflen строк [1..2]
        var segments = ToSegmentsWithProps(0, 0, "different", "abcXY", "abcXZ");

        var result = LineReader.SetAffixes(segments, 0, 2, EmptyBodyChars, EmptyBodyChars,
            hang: 1, body: false, quote: false, prefix: null, suffix: null);

        Assert.Equal(4, result.Prefix); // "abcX"
    }

    [Fact]
    public void SetAffixes_HangTooLarge_UsesFallback()
    {
        // Две строки, hang=1 → numin=2, hang+1=2 → numin <= hang+1 → fallback
        var segments = ToSegmentsWithProps(5, 3, "abc", "def");

        var result = LineReader.SetAffixes(segments, 0, 1, EmptyBodyChars, EmptyBodyChars,
            hang: 1, body: false, quote: false, prefix: null, suffix: null);

        Assert.Equal(5, result.Prefix); // fallback
        Assert.Equal(3, result.Suffix);
    }

    #endregion

    #region Тесты quote

    [Fact]
    public void SetAffixes_QuoteAugmented_AddsQuoteChars()
    {
        // Одна строка, quote=true, quote-символы после fallbackPre
        var segments = new[]
        {
            new LineSegment("> hello", new LineProp(p: 1, s: 0)), // fallbackPre=1 (">"), потом quote-символы
        };
        var quoteChars = Charset.Parse(">");

        var result = LineReader.SetAffixes(segments, 0, 0, EmptyBodyChars, quoteChars,
            hang: 0, body: false, quote: true, prefix: null, suffix: null);

        // fallbackPre=1, потом графем[1]=" " (не quote), так что augmented=1
        Assert.Equal(1, result.AugmentedFallbackPre);
        Assert.Equal(1, result.Prefix);
    }

    [Fact]
    public void SetAffixes_QuoteFalse_NoAugmentation()
    {
        // Одна строка, quote=false → augmented = fallbackPre
        var segments = new[]
        {
            new LineSegment("> hello", new LineProp(p: 1, s: 0)),
        };
        var quoteChars = Charset.Parse(">");

        var result = LineReader.SetAffixes(segments, 0, 0, EmptyBodyChars, quoteChars,
            hang: 0, body: false, quote: false, prefix: null, suffix: null);

        Assert.Equal(1, result.AugmentedFallbackPre);
        Assert.Equal(1, result.Prefix);
    }

    [Fact]
    public void SetAffixes_QuoteTrue_MultipleQuoteChars()
    {
        // Одна строка, quote=true, несколько quote-символов после fallbackPre
        var segments = new[]
        {
            new LineSegment(">>hello", new LineProp(p: 1, s: 0)),
        };
        var quoteChars = Charset.Parse(">");

        var result = LineReader.SetAffixes(segments, 0, 0, EmptyBodyChars, quoteChars,
            hang: 0, body: false, quote: true, prefix: null, suffix: null);

        // fallbackPre=1, графемы[1]='>', графемы[2]='h'(не quote) → augmented=2
        Assert.Equal(2, result.AugmentedFallbackPre);
        Assert.Equal(2, result.Prefix);
    }

    #endregion

    #region Тесты body

    [Fact]
    public void SetAffixes_BodyTrue_CompresuflenWithBody()
    {
        // Две строки, body=true → compresuflen с учётом body-символов
        var segments = ToSegmentsWithProps(0, 0, "> text1 .", "> text2 .");

        var result = LineReader.SetAffixes(segments, 0, 1, AlphaBodyChars, EmptyBodyChars,
            hang: 0, body: true, quote: false, prefix: null, suffix: null);

        // body=true: префикс до последнего non-body → "> " = 2
        Assert.Equal(2, result.Prefix);
    }

    #endregion
}

using xParLib;

namespace xParTests;

public class LineReaderTests
{
    private readonly LineReader _reader = new LineReader();

    // Charset'ы по умолчанию для тестов
    private static Charset EmptyCharset => Charset.Parse("");
    private static Charset DefaultQuoteChars => Charset.Parse("> ");
    private static Charset DefaultWhiteChars => Charset.Parse(" \f\n\r\t\v");

    #region Базовые тесты

    [Fact]
    public void ReadLines_EmptyInput_ReturnsEmptyResult()
    {
        // Act
        var result = _reader.ReadLines(
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
        var result = _reader.ReadLines(
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
        var result = _reader.ReadLines(
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
        var result = _reader.ReadLines(
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
        var result = _reader.ReadLines(
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
        var result = _reader.ReadLines(
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
        var result = _reader.ReadLines(
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
        var result = _reader.ReadLines(
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
        var result = _reader.ReadLines(
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
        var result = _reader.ReadLines(
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
        var result = _reader.ReadLines(
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
        var result = _reader.ReadLines(
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
        var result = _reader.ReadLines(
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
        var result = _reader.ReadLines(
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
        var result = _reader.ReadLines(
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
        var result = _reader.ReadLines(
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

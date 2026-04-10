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

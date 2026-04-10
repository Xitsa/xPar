using xParLib;

namespace xParTests;

public class CharsetTests
{
    #region Parse — простые символы

    [Fact]
    public void Parse_SimpleCharacters_AddsToInList()
    {
        // Act
        var charset = Charset.Parse("abc");

        // Assert
        Assert.True(charset.IsMember("a"));
        Assert.True(charset.IsMember("b"));
        Assert.True(charset.IsMember("c"));
        Assert.False(charset.IsMember("d"));
    }

    [Fact]
    public void Parse_EmptyString_NoMembers()
    {
        // Act
        var charset = Charset.Parse("");

        // Assert
        Assert.False(charset.IsMember("a"));
        Assert.False(charset.IsMember(" "));
    }

    #endregion

    #region Parse — escape-последовательности

    [Theory]
    [InlineData("__", "_")]
    [InlineData("_s", " ")]
    [InlineData("_b", "\\")]
    [InlineData("_q", "'")]
    [InlineData("_Q", "\"")]
    public void Parse_SingleCharEscapes_CorrectCharacter(string input, string expected)
    {
        // Act
        var charset = Charset.Parse(input);

        // Assert
        Assert.True(charset.IsMember(expected));
    }

    #endregion

    #region Parse — hex-последовательности

    [Theory]
    [InlineData("_x41", "A")]    // U+0041
    [InlineData("_x7a", "z")]    // U+007A
    [InlineData("_x30", "0")]    // U+0030
    public void Parse_TwoDigitHex_CorrectCharacter(string input, string expected)
    {
        // Act
        var charset = Charset.Parse(input);

        // Assert
        Assert.True(charset.IsMember(expected));
    }

    [Theory]
    [InlineData("_X00A9", "©")]   // U+00A9 copyright
    [InlineData("_X2603", "☃")]   // U+2603 snowman
    [InlineData("_X00E9", "é")]   // U+00E9 e acute
    public void Parse_FourDigitHex_CorrectCharacter(string input, string expected)
    {
        // Act
        var charset = Charset.Parse(input);

        // Assert
        Assert.True(charset.IsMember(expected));
    }

    #endregion

    #region Parse — классы символов (флаги)

    [Fact]
    public void Parse_UppercaseFlag_MatchesLatinUppercase()
    {
        // Act
        var charset = Charset.Parse("_A");

        // Assert
        Assert.True(charset.IsMember("A"));
        Assert.True(charset.IsMember("Z"));
        Assert.False(charset.IsMember("a"));
        Assert.False(charset.IsMember("0"));
    }

    [Fact]
    public void Parse_LowercaseFlag_MatchesLatinLowercase()
    {
        // Act
        var charset = Charset.Parse("_a");

        // Assert
        Assert.True(charset.IsMember("a"));
        Assert.True(charset.IsMember("z"));
        Assert.False(charset.IsMember("A"));
        Assert.False(charset.IsMember("0"));
    }

    [Fact]
    public void Parse_DigitFlag_MatchesDigits()
    {
        // Act
        var charset = Charset.Parse("_0");

        // Assert
        Assert.True(charset.IsMember("0"));
        Assert.True(charset.IsMember("5"));
        Assert.True(charset.IsMember("9"));
        Assert.False(charset.IsMember("a"));
    }

    [Fact]
    public void Parse_SpaceFlag_MatchesWhitespace()
    {
        // Act
        var charset = Charset.Parse("_S");

        // Assert
        Assert.True(charset.IsMember(" "));
        Assert.True(charset.IsMember("\t"));
        Assert.True(charset.IsMember("\n"));
        Assert.True(charset.IsMember("\r"));
        Assert.False(charset.IsMember("a"));
    }

    #endregion

    #region Unicode — кириллица и другие алфавиты

    [Fact]
    public void Parse_UppercaseFlag_MatchesCyrillicUppercase()
    {
        // Act
        var charset = Charset.Parse("_A");

        // Assert
        Assert.True(charset.IsMember("Ф"));
        Assert.True(charset.IsMember("Я"));
        Assert.True(charset.IsMember("А"));
    }

    [Fact]
    public void Parse_LowercaseFlag_MatchesCyrillicLowercase()
    {
        // Act
        var charset = Charset.Parse("_a");

        // Assert
        Assert.True(charset.IsMember("ф"));
        Assert.True(charset.IsMember("я"));
        Assert.True(charset.IsMember("а"));
    }

    [Fact]
    public void Parse_UppercaseFlag_MatchesGreekUppercase()
    {
        // Act
        var charset = Charset.Parse("_A");

        // Assert
        Assert.True(charset.IsMember("Ω")); // U+03A9
        Assert.True(charset.IsMember("Σ")); // U+03A3
    }

    #endregion

    #region Parse — комбинации

    [Fact]
    public void Parse_CombinedFlagsAndCharacters_WorksCorrectly()
    {
        // Act
        var charset = Charset.Parse("_A_a_0");

        // Assert
        Assert.True(charset.IsMember("A"));
        Assert.True(charset.IsMember("z"));
        Assert.True(charset.IsMember("5"));
        Assert.False(charset.IsMember(" "));
        Assert.False(charset.IsMember("@"));
    }

    [Fact]
    public void Parse_MixedCharactersAndEscapes_WorksCorrectly()
    {
        // Act — .?!: это просто символы, без escape
        var charset = Charset.Parse(".?!:");

        // Assert
        Assert.True(charset.IsMember("."));
        Assert.True(charset.IsMember("?"));
        Assert.True(charset.IsMember(":"));
        Assert.True(charset.IsMember("!"));
        Assert.False(charset.IsMember("a"));
    }

    #endregion

    #region Parse — ошибки

    [Theory]
    [InlineData("_")]
    [InlineData("abc_")]
    public void Parse_TrailingUnderscore_ThrowsException(string input)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Charset.Parse(input));
    }

    [Theory]
    [InlineData("_x")]
    [InlineData("_x1")]
    [InlineData("_xGG")]
    public void Parse_InvalidTwoDigitHex_ThrowsException(string input)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Charset.Parse(input));
    }

    [Theory]
    [InlineData("_X")]
    [InlineData("_X123")]
    [InlineData("_XGGGG")]
    public void Parse_InvalidFourDigitHex_ThrowsException(string input)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Charset.Parse(input));
    }

    [Fact]
    public void Parse_InvalidEscape_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Charset.Parse("_z"));
    }

    #endregion

    #region Операции над множествами

    [Fact]
    public void Union_TwoCharsets_CombinesMembers()
    {
        // Arrange
        var cs1 = Charset.Parse("abc");
        var cs2 = Charset.Parse("def");

        // Act
        var union = cs1.Union(cs2);

        // Assert
        Assert.True(union.IsMember("a"));
        Assert.True(union.IsMember("d"));
        Assert.False(union.IsMember("g"));
    }

    [Fact]
    public void Union_CharsetWithFlags_IncludesFlags()
    {
        // Arrange
        var cs1 = Charset.Parse("_A");
        var cs2 = Charset.Parse("_0");

        // Act
        var union = cs1.Union(cs2);

        // Assert
        Assert.True(union.IsMember("A"));
        Assert.True(union.IsMember("5"));
        Assert.False(union.IsMember("a"));
    }

    [Fact]
    public void Difference_TwoCharsets_RemovesMembers()
    {
        // Arrange
        var cs1 = Charset.Parse("abcdef");
        var cs2 = Charset.Parse("bcd");

        // Act
        var diff = cs1.Difference(cs2);

        // Assert
        Assert.True(diff.IsMember("a"));
        Assert.True(diff.IsMember("e"));
        Assert.True(diff.IsMember("f"));
        Assert.False(diff.IsMember("b"));
        Assert.False(diff.IsMember("c"));
    }

    [Fact]
    public void Add_ModifiesExistingCharset()
    {
        // Arrange
        var cs1 = Charset.Parse("abc");
        var cs2 = Charset.Parse("def");

        // Act
        cs1.Add(cs2);

        // Assert
        Assert.True(cs1.IsMember("a"));
        Assert.True(cs1.IsMember("d"));
        Assert.False(cs1.IsMember("g"));
    }

    [Fact]
    public void Remove_ModifiesExistingCharset()
    {
        // Arrange
        var cs1 = Charset.Parse("abcdef");
        var cs2 = Charset.Parse("bcd");

        // Act
        cs1.Remove(cs2);

        // Assert
        Assert.True(cs1.IsMember("a"));
        Assert.True(cs1.IsMember("e"));
        Assert.True(cs1.IsMember("f"));
        Assert.False(cs1.IsMember("b"));
    }

    [Fact]
    public void Clone_CreatesIndependentCopy()
    {
        // Arrange
        var cs1 = Charset.Parse("abc");

        // Act
        var clone = cs1.Clone();

        // Assert
        Assert.True(clone.IsMember("a"));
        
        // Изменение оригинала не влияет на клон
        cs1.Add(Charset.Parse("def"));
        Assert.True(clone.IsMember("a"));
        Assert.False(clone.IsMember("d"));
    }

    [Fact]
    public void Replace_CompletelyReplacesContents()
    {
        // Arrange
        var cs1 = Charset.Parse("abc");
        var cs2 = Charset.Parse("xyz");

        // Act
        cs1.Replace(cs2);

        // Assert
        Assert.True(cs1.IsMember("x"));
        Assert.True(cs1.IsMember("y"));
        Assert.True(cs1.IsMember("z"));
        Assert.False(cs1.IsMember("a"));
        Assert.False(cs1.IsMember("b"));
    }

    #endregion

    #region ParOptions.Parse с charset-опциями

    [Fact]
    public void Parse_CharsetEqualsOption_ReplacesCharset()
    {
        // Arrange
        var bodyChars = Charset.Parse("");
        var options = ParOptions.Parse(new[] { "B=abc" },
            bodyChars, Charset.Parse(""), Charset.Parse(""),
            Charset.Parse(""), Charset.Parse(""));

        // Assert
        Assert.NotNull(options.BodyChars);
        Assert.True(options.BodyChars.IsMember("a"));
        Assert.True(options.BodyChars.IsMember("b"));
        Assert.True(options.BodyChars.IsMember("c"));
    }

    [Fact]
    public void Parse_CharsetPlusOption_AddsToCharset()
    {
        // Arrange
        var bodyChars = Charset.Parse("abc");
        var options = ParOptions.Parse(new[] { "B+def" },
            bodyChars, Charset.Parse(""), Charset.Parse(""),
            Charset.Parse(""), Charset.Parse(""));

        // Assert
        Assert.NotNull(options.BodyChars);
        Assert.True(options.BodyChars.IsMember("a"));
        Assert.True(options.BodyChars.IsMember("d"));
        Assert.True(options.BodyChars.IsMember("f"));
    }

    [Fact]
    public void Parse_CharsetMinusOption_RemovesFromCharset()
    {
        // Arrange
        var quoteChars = Charset.Parse("> _s");
        var options = ParOptions.Parse(new[] { "Q-_s" },
            Charset.Parse(""), Charset.Parse(""), quoteChars,
            Charset.Parse(""), Charset.Parse(""));

        // Assert
        Assert.NotNull(options.QuoteChars);
        Assert.True(options.QuoteChars.IsMember(">"));
        Assert.False(options.QuoteChars.IsMember(" "));
    }

    [Fact]
    public void Parse_MultipleCharsetOperations_ApplySequentially()
    {
        // Arrange
        var bodyChars = Charset.Parse("abc");
        var options = ParOptions.Parse(new[] { "B+de", "B-f" },
            bodyChars, Charset.Parse(""), Charset.Parse(""),
            Charset.Parse(""), Charset.Parse(""));

        // Assert - сначала добавили de, но f не было, значит abcde
        Assert.NotNull(options.BodyChars);
        Assert.True(options.BodyChars.IsMember("a"));
        Assert.True(options.BodyChars.IsMember("d"));
        Assert.True(options.BodyChars.IsMember("e"));
        Assert.False(options.BodyChars.IsMember("f"));
    }

    [Fact]
    public void Parse_AllCharsetTypes_WorkCorrectly()
    {
        // Arrange & Act
        var options = ParOptions.Parse(new[] {
            "B=_A_a",       // body = letters
            "P=_0",         // protect = digits
            "Q=>",          // quote = >
            "W=_S",         // white = whitespace
            "Z=.?!:"        // terminal = punctuation
        },
            Charset.Parse(""), Charset.Parse(""), Charset.Parse("> "),
            Charset.Parse(" \f\n\r\t\v"), Charset.Parse(".?!:"));

        // Assert
        Assert.NotNull(options.BodyChars);
        Assert.NotNull(options.ProtectChars);
        Assert.NotNull(options.QuoteChars);
        Assert.NotNull(options.WhiteChars);
        Assert.NotNull(options.TerminalChars);

        Assert.True(options.BodyChars.IsMember("A"));
        Assert.True(options.BodyChars.IsMember("z"));
        Assert.True(options.ProtectChars.IsMember("5"));
        Assert.True(options.QuoteChars.IsMember(">"));
        Assert.True(options.WhiteChars.IsMember("\t"));
        Assert.True(options.TerminalChars.IsMember("."));
    }

    #endregion
}

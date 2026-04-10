using xParLib;

namespace xParTests;

public class ParOptionsTests
{
    [Fact]
    public void Parse_DefaultValues_ReturnsCorrectDefaults()
    {
        // Act
        var options = ParOptions.Parse(Array.Empty<string>());

        // Assert
        Assert.Equal(0, options.Hang);
        Assert.Null(options.Prefix);
        Assert.Equal(0, options.Repeat);
        Assert.Null(options.Suffix);
        Assert.Equal(1, options.Tab);
        Assert.Equal(72, options.Width);
        Assert.False(options.Body);
        Assert.False(options.Cap);
        Assert.False(options.Div);
        Assert.False(options.Err);
        Assert.False(options.Expel);
        Assert.False(options.Fit);
        Assert.False(options.Guess);
        Assert.False(options.Invis);
        Assert.False(options.Just);
        Assert.False(options.Last);
        Assert.False(options.Quote);
        Assert.False(options.Report);
        Assert.False(options.Touch);
        Assert.False(options.Help);
        Assert.False(options.Version);
    }

    [Theory]
    [InlineData("h", 1)]
    [InlineData("h0", 0)]
    [InlineData("h5", 5)]
    [InlineData("h10", 10)]
    public void Parse_Hang_ParsesCorrectly(string arg, int expected)
    {
        // Act
        var options = ParOptions.Parse(new[] { arg });

        // Assert
        Assert.Equal(expected, options.Hang);
    }

    [Theory]
    [InlineData("p", null)]
    [InlineData("p0", 0)]
    [InlineData("p5", 5)]
    [InlineData("5", 5)]
    [InlineData("p12", 12)]
    public void Parse_Prefix_ParsesCorrectly(string arg, int? expected)
    {
        // Act
        var options = ParOptions.Parse(new[] { arg });

        // Assert
        Assert.Equal(expected, options.Prefix);
    }

    [Theory]
    [InlineData("r", 3)]
    [InlineData("r0", 0)]
    [InlineData("r5", 5)]
    public void Parse_Repeat_ParsesCorrectly(string arg, int expected)
    {
        // Act
        var options = ParOptions.Parse(new[] { arg });

        // Assert
        Assert.Equal(expected, options.Repeat);
    }

    [Theory]
    [InlineData("s", null)]
    [InlineData("s0", 0)]
    [InlineData("s3", 3)]
    public void Parse_Suffix_ParsesCorrectly(string arg, int? expected)
    {
        // Act
        var options = ParOptions.Parse(new[] { arg });

        // Assert
        Assert.Equal(expected, options.Suffix);
    }

    [Theory]
    [InlineData("T", 8)]
    [InlineData("T1", 1)]
    [InlineData("T4", 4)]
    public void Parse_Tab_ParsesCorrectly(string arg, int expected)
    {
        // Act
        var options = ParOptions.Parse(new[] { arg });

        // Assert
        Assert.Equal(expected, options.Tab);
    }

    [Theory]
    [InlineData("w", 79)]
    [InlineData("w0", 0)]
    [InlineData("78", 78)]
    [InlineData("w80", 80)]
    [InlineData("w120", 120)]
    public void Parse_Width_ParsesCorrectly(string arg, int expected)
    {
        // Act
        var options = ParOptions.Parse(new[] { arg });

        // Assert
        Assert.Equal(expected, options.Width);
    }

    [Theory]
    [InlineData("b")]
    [InlineData("b1")]
    public void Parse_Body_SetsToTrue(string arg)
    {
        // Act
        var options = ParOptions.Parse(new[] { arg });

        // Assert
        Assert.True(options.Body);
    }

    [Theory]
    [InlineData("c")]
    [InlineData("d")]
    [InlineData("E")]
    [InlineData("e")]
    [InlineData("f")]
    [InlineData("g")]
    [InlineData("i")]
    [InlineData("j")]
    [InlineData("l")]
    [InlineData("q")]
    [InlineData("R")]
    [InlineData("t")]
    public void Parse_BooleanFlags_SetsToTrue(string arg)
    {
        // Act
        var options = ParOptions.Parse(new[] { arg });

        // Assert
        switch (arg)
        {
            case "c": Assert.True(options.Cap); break;
            case "d": Assert.True(options.Div); break;
            case "E": Assert.True(options.Err); break;
            case "e": Assert.True(options.Expel); break;
            case "f": Assert.True(options.Fit); break;
            case "g": Assert.True(options.Guess); break;
            case "i": Assert.True(options.Invis); break;
            case "j": Assert.True(options.Just); break;
            case "l": Assert.True(options.Last); break;
            case "q": Assert.True(options.Quote); break;
            case "R": Assert.True(options.Report); break;
            case "t": Assert.True(options.Touch); break;
        }
    }

    [Fact]
    public void Parse_Help_SetsHelpToTrue()
    {
        // Act
        var options = ParOptions.Parse(new[] { "help" });

        // Assert
        Assert.True(options.Help);
    }

    [Fact]
    public void Parse_Version_SetsVersionToTrue()
    {
        // Act
        var options = ParOptions.Parse(new[] { "version" });

        // Assert
        Assert.True(options.Version);
    }

    [Theory]
    [InlineData("5", 5)]
    [InlineData("8", 8)]
    [InlineData("0", 0)]
    public void Parse_NumberWithoutFlag_SetsPrefix(string arg, int expectedPrefix)
    {
        // Act
        var options = ParOptions.Parse(new[] { arg });

        // Assert
        Assert.Equal(expectedPrefix, options.Prefix);
        Assert.Equal(72, options.Width); // по умолчанию
    }

    [Theory]
    [InlineData("9", 9)]
    [InlineData("72", 72)]
    [InlineData("100", 100)]
    public void Parse_NumberWithoutFlag_SetsWidth(string arg, int expectedWidth)
    {
        // Act
        var options = ParOptions.Parse(new[] { arg });

        // Assert
        Assert.Null(options.Prefix);
        Assert.Equal(expectedWidth, options.Width);
    }

    [Fact]
    public void Parse_MultipleArgs_ParsesAllCorrectly()
    {
        // Act
        var options = ParOptions.Parse(new[] { "w80", "h2", "f", "q" });

        // Assert
        Assert.Equal(80, options.Width);
        Assert.Equal(2, options.Hang);
        Assert.True(options.Fit);
        Assert.True(options.Quote);
    }

    [Fact]
    public void Parse_MultipleArgsInOneArg_ParsesAllCorrectly()
    {
        // Act
        var options = ParOptions.Parse(new[] { "80h2fqjre"});

        // Assert
        Assert.Equal(80, options.Width);
        Assert.Equal(2, options.Hang);
        Assert.True(options.Fit);
        Assert.True(options.Quote);
        Assert.True(options.Expel);
        Assert.True(options.Just);
        Assert.Equal(3, options.Repeat);
    }

        [Fact]
    public void Parse_Touch_Default_IsFitOrLast()
    {
        // Act - без флагов
        var options1 = ParOptions.Parse(Array.Empty<string>());

        // Assert
        Assert.False(options1.Touch);

        // Act - с fit
        var options2 = ParOptions.Parse(new[] { "f" });

        // Assert
        Assert.True(options2.Touch);

        // Act - с last
        var options3 = ParOptions.Parse(new[] { "l" });

        // Assert
        Assert.True(options3.Touch);
    }

    [Fact]
    public void Parse_LeadingDash_Ignored()
    {
        // Act
        var options = ParOptions.Parse(new[] { "-w80", "-f" });

        // Assert
        Assert.Equal(80, options.Width);
        Assert.True(options.Fit);
    }

    [Fact]
    public void Parse_LastValueWins_WhenParameterSetMultipleTimes()
    {
        // Act
        var options = ParOptions.Parse(new[] { "w50", "w80", "w100" });

        // Assert
        Assert.Equal(100, options.Width);
    }

    [Fact]
    public void Parse_LastValueWins_WhenBooleanParameterToggled()
    {
        // Act
        var options = ParOptions.Parse(new[] { "f", "t0", "f" });

        // Assert
        Assert.True(options.Fit);
        Assert.False(options.Touch); // t0 установлен после f
    }

    [Fact]
    public void Parse_Touch_ExplicitValueNotOverriddenByFitOrLast()
    {
        // Act - явно установлен t0, но есть f и l
        var options = ParOptions.Parse(new[] { "f", "l", "t0" });

        // Assert
        Assert.True(options.Fit);
        Assert.True(options.Last);
        Assert.False(options.Touch); // явно заданное значение не перезаписывается
    }

    [Fact]
    public void Parse_HangAndQuote_AssignedBeforePrefix()
    {
        // Эта зависимость означает, что hang и quote должны быть определены
        // до того, как будет вычислено значение prefix по умолчанию.
        // На текущем шаге prefix остаётся null, если не задан явно.
        // Вычисление prefix на основе hang произойдёт при обновлении Transform.
        // Этот тест подтверждает, что hang определён до проверки prefix.

        // Act
        var options = ParOptions.Parse(new[] { "h5" });

        // Assert
        Assert.Equal(5, options.Hang);
        Assert.Null(options.Prefix); // prefix вычисляется позже, не здесь
    }

    [Fact]
    public void Parse_FitAndLast_AssignedBeforeTouch()
    {
        // fit и last должны быть определены до вычисления touch по умолчанию.
        // Touch = fit || last, если touch не задан явно.

        // Act - оба true
        var options1 = ParOptions.Parse(new[] { "f", "l" });

        // Assert
        Assert.True(options1.Fit);
        Assert.True(options1.Last);
        Assert.True(options1.Touch); // = fit || last

        // Act - только last
        var options2 = ParOptions.Parse(new[] { "l" });
        Assert.True(options2.Touch);

        // Act - ни одного
        var options3 = ParOptions.Parse(Array.Empty<string>());
        Assert.False(options3.Touch);
    }
}

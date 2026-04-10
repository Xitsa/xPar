using xParLib;

namespace xParTests;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        Assert.True(true);
    }

    [Theory]
    [InlineData("Hello World", "Hello World[5, 5]")]
    [InlineData("Test", "Test[4]")]
    [InlineData("", "[0]")]
    [InlineData("а б в г д е ё ж з и и́ кl 👨🏻‍❤️‍💋‍👨🏻 Ull", "а б в г д е ё ж з и и́ кl 👨🏻‍❤️‍💋‍👨🏻 Ull[1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1, 3]")]
    [InlineData("コ コ́", "コ コ́[2, 2]")]
    public void Transform_ProcessesLines_Correctly(string input, string expected)
    {
        // Arrange
        var transformer = new StringTransformer();
        var inputLines = new[] { input };

        // Act
        var result = transformer.Transform(inputLines);

        // Assert
        Assert.Single(result);
        Assert.Equal(expected, result[0]);
    }
}

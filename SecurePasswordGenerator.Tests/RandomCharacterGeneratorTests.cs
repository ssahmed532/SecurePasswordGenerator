using SecurePasswordGenerator.Utilities;

namespace SecurePasswordGenerator.Tests;

public class RandomCharacterGeneratorTests
{
    private const string SpecialCharacters = "!@#$%^&*()-_=+";

    [Fact]
    public void GetRandomUppercase_ReturnsUppercaseLetter()
    {
        for (int i = 0; i < 100; i++)
        {
            char c = RandomCharacterGenerator.GetRandomUppercase();
            Assert.True(char.IsUpper(c), $"Expected uppercase letter but got '{c}'");
        }
    }

    [Fact]
    public void GetRandomLowercase_ReturnsLowercaseLetter()
    {
        for (int i = 0; i < 100; i++)
        {
            char c = RandomCharacterGenerator.GetRandomLowercase();
            Assert.True(char.IsLower(c), $"Expected lowercase letter but got '{c}'");
        }
    }

    [Fact]
    public void GetRandomDigit_ReturnsDigit()
    {
        for (int i = 0; i < 100; i++)
        {
            char c = RandomCharacterGenerator.GetRandomDigit();
            Assert.True(char.IsDigit(c), $"Expected digit but got '{c}'");
        }
    }

    [Fact]
    public void GetRandomSpecialCharacter_ReturnsSpecialCharacter()
    {
        for (int i = 0; i < 100; i++)
        {
            char c = RandomCharacterGenerator.GetRandomSpecialCharacter();
            Assert.Contains(c, SpecialCharacters);
        }
    }

    [Fact]
    public void GetRandomUppercase_ProducesVariety()
    {
        var chars = Enumerable.Range(0, 200)
            .Select(_ => RandomCharacterGenerator.GetRandomUppercase())
            .Distinct()
            .ToList();

        Assert.True(chars.Count > 1, "Expected multiple distinct uppercase letters");
    }

    [Fact]
    public void GetRandomLowercase_ProducesVariety()
    {
        var chars = Enumerable.Range(0, 200)
            .Select(_ => RandomCharacterGenerator.GetRandomLowercase())
            .Distinct()
            .ToList();

        Assert.True(chars.Count > 1, "Expected multiple distinct lowercase letters");
    }

    [Fact]
    public void GetRandomDigit_ProducesVariety()
    {
        var chars = Enumerable.Range(0, 200)
            .Select(_ => RandomCharacterGenerator.GetRandomDigit())
            .Distinct()
            .ToList();

        Assert.True(chars.Count > 1, "Expected multiple distinct digits");
    }

    [Fact]
    public void GetRandomSpecialCharacter_ProducesVariety()
    {
        var chars = Enumerable.Range(0, 200)
            .Select(_ => RandomCharacterGenerator.GetRandomSpecialCharacter())
            .Distinct()
            .ToList();

        Assert.True(chars.Count > 1, "Expected multiple distinct special characters");
    }

    [Fact]
    public void GetRandomUppercase_ReturnsWithinAsciiRange()
    {
        for (int i = 0; i < 200; i++)
        {
            char c = RandomCharacterGenerator.GetRandomUppercase();
            Assert.InRange(c, 'A', 'Z');
        }
    }

    [Fact]
    public void GetRandomLowercase_ReturnsWithinAsciiRange()
    {
        for (int i = 0; i < 200; i++)
        {
            char c = RandomCharacterGenerator.GetRandomLowercase();
            Assert.InRange(c, 'a', 'z');
        }
    }

    [Fact]
    public void GetRandomDigit_ReturnsWithinAsciiRange()
    {
        for (int i = 0; i < 200; i++)
        {
            char c = RandomCharacterGenerator.GetRandomDigit();
            Assert.InRange(c, '0', '9');
        }
    }

    [Fact]
    public void GetRandomUppercase_CoversFullRange()
    {
        var chars = Enumerable.Range(0, 5000)
            .Select(_ => RandomCharacterGenerator.GetRandomUppercase())
            .Distinct()
            .ToHashSet();

        Assert.Equal(26, chars.Count);
    }

    [Fact]
    public void GetRandomLowercase_CoversFullRange()
    {
        var chars = Enumerable.Range(0, 5000)
            .Select(_ => RandomCharacterGenerator.GetRandomLowercase())
            .Distinct()
            .ToHashSet();

        Assert.Equal(26, chars.Count);
    }

    [Fact]
    public void GetRandomDigit_CoversFullRange()
    {
        var chars = Enumerable.Range(0, 5000)
            .Select(_ => RandomCharacterGenerator.GetRandomDigit())
            .Distinct()
            .ToHashSet();

        Assert.Equal(10, chars.Count);
    }

    [Fact]
    public void GetRandomSpecialCharacter_CoversFullRange()
    {
        var chars = Enumerable.Range(0, 5000)
            .Select(_ => RandomCharacterGenerator.GetRandomSpecialCharacter())
            .Distinct()
            .ToHashSet();

        Assert.Equal(SpecialCharacters.Length, chars.Count);
    }
}

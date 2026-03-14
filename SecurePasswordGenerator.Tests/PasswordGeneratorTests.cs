using SecurePasswordGenerator.Models;
using SecurePasswordGenerator.Services;

namespace SecurePasswordGenerator.Tests;

/// <summary>
/// Tests for PasswordGenerator.
/// Tests marked [Fact(Skip = "TODO: ...")] validate behavior planned in TODO.md.
/// Remove the Skip once the corresponding feature is implemented.
/// </summary>
public class PasswordGeneratorTests
{
    private const string SpecialCharacters = "!@#$%^&*()-_=+";

    [Fact]
    public void GeneratePassword_WithDefaultPolicy_ReturnsCorrectLength()
    {
        var generator = new PasswordGenerator(new PasswordPolicy());

        string password = generator.GeneratePassword();

        Assert.Equal(16, password.Length);
    }

    [Fact]
    public void GeneratePassword_WithDefaultPolicy_ContainsUppercase()
    {
        var generator = new PasswordGenerator(new PasswordPolicy());

        string password = generator.GeneratePassword();

        Assert.Contains(password, c => char.IsUpper(c));
    }

    [Fact]
    public void GeneratePassword_WithDefaultPolicy_ContainsLowercase()
    {
        var generator = new PasswordGenerator(new PasswordPolicy());

        string password = generator.GeneratePassword();

        Assert.Contains(password, c => char.IsLower(c));
    }

    [Fact]
    public void GeneratePassword_WithDefaultPolicy_ContainsDigit()
    {
        var generator = new PasswordGenerator(new PasswordPolicy());

        string password = generator.GeneratePassword();

        Assert.Contains(password, c => char.IsDigit(c));
    }

    [Fact]
    public void GeneratePassword_WithDefaultPolicy_ContainsSpecialCharacter()
    {
        var generator = new PasswordGenerator(new PasswordPolicy());

        string password = generator.GeneratePassword();

        Assert.Contains(password, c => SpecialCharacters.Contains(c));
    }

    [Fact]
    public void GeneratePassword_WithCustomLength_ReturnsCorrectLength()
    {
        var policy = new PasswordPolicy { MinimumLength = 24, MaximumLength = 24 };
        var generator = new PasswordGenerator(policy);

        string password = generator.GeneratePassword();

        Assert.Equal(24, password.Length);
    }

    [Fact]
    public void GeneratePassword_WithMinimumLength12_Succeeds()
    {
        var policy = new PasswordPolicy { MinimumLength = 12, MaximumLength = 12 };
        var generator = new PasswordGenerator(policy);

        string password = generator.GeneratePassword();

        Assert.Equal(12, password.Length);
    }

    [Fact]
    public void GeneratePassword_WithLengthBelow12_ThrowsArgumentException()
    {
        var policy = new PasswordPolicy { MinimumLength = 8, MaximumLength = 8 };
        var generator = new PasswordGenerator(policy);

        var ex = Assert.Throws<ArgumentException>(() => generator.GeneratePassword());
        Assert.Contains("PCI-DSS", ex.Message);
    }

    [Fact]
    public void GeneratePassword_ProducesUniquePasswords()
    {
        var generator = new PasswordGenerator(new PasswordPolicy());

        var passwords = Enumerable.Range(0, 50)
            .Select(_ => generator.GeneratePassword())
            .ToHashSet();

        Assert.True(passwords.Count == 50, "Expected all 50 generated passwords to be unique");
    }

    [Fact]
    public void GeneratePassword_ContainsOnlyValidCharacters()
    {
        var generator = new PasswordGenerator(new PasswordPolicy());

        for (int i = 0; i < 20; i++)
        {
            string password = generator.GeneratePassword();
            foreach (char c in password)
            {
                bool isValid = char.IsUpper(c) || char.IsLower(c) ||
                               char.IsDigit(c) || SpecialCharacters.Contains(c);
                Assert.True(isValid, $"Invalid character '{c}' found in password '{password}'");
            }
        }
    }

    [Fact]
    public void GeneratePassword_WithLargeLength_ReturnsCorrectLength()
    {
        var policy = new PasswordPolicy { MinimumLength = 64, MaximumLength = 64 };
        var generator = new PasswordGenerator(policy);

        string password = generator.GeneratePassword();

        Assert.Equal(64, password.Length);
    }

    [Fact]
    public void GeneratePassword_ShufflesCharacters()
    {
        // Generate many passwords and verify the first character isn't always uppercase
        // (the generator adds uppercase first, so if shuffling works, positions should vary)
        var generator = new PasswordGenerator(new PasswordPolicy());

        var firstChars = Enumerable.Range(0, 50)
            .Select(_ => generator.GeneratePassword()[0])
            .ToList();

        bool allSameType = firstChars.All(c => char.IsUpper(c));
        Assert.False(allSameType, "Expected shuffling to vary the first character type");
    }

    [Fact]
    public void GeneratePassword_WithMinimumLength11_ThrowsArgumentException()
    {
        var policy = new PasswordPolicy { MinimumLength = 11, MaximumLength = 11 };
        var generator = new PasswordGenerator(policy);

        Assert.Throws<ArgumentException>(() => generator.GeneratePassword());
    }

    [Fact]
    public void GeneratePassword_WithMinimumLengthZero_ThrowsArgumentException()
    {
        var policy = new PasswordPolicy { MinimumLength = 0, MaximumLength = 0 };
        var generator = new PasswordGenerator(policy);

        Assert.Throws<ArgumentException>(() => generator.GeneratePassword());
    }

    [Fact]
    public void GeneratePassword_WithNegativeMinimumLength_ThrowsArgumentException()
    {
        var policy = new PasswordPolicy { MinimumLength = -5, MaximumLength = -5 };
        var generator = new PasswordGenerator(policy);

        Assert.Throws<ArgumentException>(() => generator.GeneratePassword());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(11)]
    public void GeneratePassword_WithLengthBelowPciMinimum_ThrowsArgumentException(int length)
    {
        var policy = new PasswordPolicy { MinimumLength = length, MaximumLength = length };
        var generator = new PasswordGenerator(policy);

        var ex = Assert.Throws<ArgumentException>(() => generator.GeneratePassword());
        Assert.Contains("PCI-DSS", ex.Message);
    }

    [Fact]
    public void GeneratePassword_SameInstance_ProducesDifferentPasswords()
    {
        var generator = new PasswordGenerator(new PasswordPolicy());

        string first = generator.GeneratePassword();
        string second = generator.GeneratePassword();

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void GeneratePassword_AtMinimumBoundary12_ContainsAllCharacterTypes()
    {
        var policy = new PasswordPolicy { MinimumLength = 12, MaximumLength = 12 };
        var generator = new PasswordGenerator(policy);

        // Run multiple times since the password is short and we want to ensure guarantees hold
        for (int i = 0; i < 20; i++)
        {
            string password = generator.GeneratePassword();
            Assert.Contains(password, c => char.IsUpper(c));
            Assert.Contains(password, c => char.IsLower(c));
            Assert.Contains(password, c => char.IsDigit(c));
            Assert.Contains(password, c => SpecialCharacters.Contains(c));
        }
    }

    [Fact]
    public void GeneratePassword_AlwaysContainsAllRequiredTypes_Over100Generations()
    {
        var generator = new PasswordGenerator(new PasswordPolicy());

        for (int i = 0; i < 100; i++)
        {
            string password = generator.GeneratePassword();
            Assert.True(password.Any(char.IsUpper), $"Missing uppercase in: {password}");
            Assert.True(password.Any(char.IsLower), $"Missing lowercase in: {password}");
            Assert.True(password.Any(char.IsDigit), $"Missing digit in: {password}");
            Assert.True(password.Any(c => SpecialCharacters.Contains(c)), $"Missing special char in: {password}");
        }
    }

    [Fact]
    public void GeneratePassword_IgnoresRequireUppercaseFalse_StillContainsUppercase()
    {
        // Current implementation always adds one uppercase regardless of policy flag.
        // This test documents that behavior.
        var policy = new PasswordPolicy { RequireUppercase = false };
        var generator = new PasswordGenerator(policy);

        string password = generator.GeneratePassword();

        Assert.Contains(password, c => char.IsUpper(c));
    }

    [Fact]
    public void GeneratePassword_IgnoresRequireLowercaseFalse_StillContainsLowercase()
    {
        var policy = new PasswordPolicy { RequireLowercase = false };
        var generator = new PasswordGenerator(policy);

        string password = generator.GeneratePassword();

        Assert.Contains(password, c => char.IsLower(c));
    }

    [Fact]
    public void GeneratePassword_IgnoresRequireNumberFalse_StillContainsDigit()
    {
        var policy = new PasswordPolicy { RequireNumber = false };
        var generator = new PasswordGenerator(policy);

        string password = generator.GeneratePassword();

        Assert.Contains(password, c => char.IsDigit(c));
    }

    [Fact]
    public void GeneratePassword_IgnoresRequireSpecialCharacterFalse_StillContainsSpecial()
    {
        var policy = new PasswordPolicy { RequireSpecialCharacter = false };
        var generator = new PasswordGenerator(policy);

        string password = generator.GeneratePassword();

        Assert.Contains(password, c => SpecialCharacters.Contains(c));
    }

    [Fact]
    public void GeneratePassword_PasswordIsNotEmpty()
    {
        var generator = new PasswordGenerator(new PasswordPolicy());

        string password = generator.GeneratePassword();

        Assert.NotNull(password);
        Assert.NotEmpty(password);
    }

    [Fact]
    public void GeneratePassword_WithLength100_ContainsAllCharacterTypes()
    {
        var policy = new PasswordPolicy { MinimumLength = 100, MaximumLength = 100 };
        var generator = new PasswordGenerator(policy);

        string password = generator.GeneratePassword();

        Assert.Equal(100, password.Length);
        Assert.Contains(password, c => char.IsUpper(c));
        Assert.Contains(password, c => char.IsLower(c));
        Assert.Contains(password, c => char.IsDigit(c));
        Assert.Contains(password, c => SpecialCharacters.Contains(c));
    }

    // ---------------------------------------------------------------
    // TODO: Five generation rules (TODO.md)
    // These tests validate the rules documented in the TODO comments
    // inside PasswordGenerator.GeneratePassword(). They are skipped
    // until the rules are implemented.
    // ---------------------------------------------------------------

    [Fact(Skip = "TODO: Rule 1 — password must start with a letter")]
    public void GeneratePassword_ShouldStartWithLetter()
    {
        var generator = new PasswordGenerator(new PasswordPolicy());

        for (int i = 0; i < 100; i++)
        {
            string password = generator.GeneratePassword();
            Assert.True(char.IsLetter(password[0]),
                $"Password should start with a letter but started with '{password[0]}': {password}");
        }
    }

    [Fact(Skip = "TODO: Rule 2 — no consecutive duplicate characters")]
    public void GeneratePassword_ShouldNotContainConsecutiveDuplicates()
    {
        var generator = new PasswordGenerator(new PasswordPolicy());

        for (int i = 0; i < 100; i++)
        {
            string password = generator.GeneratePassword();
            for (int j = 1; j < password.Length; j++)
            {
                Assert.NotEqual(password[j - 1], password[j]);
            }
        }
    }

    [Fact(Skip = "TODO: Rule 3 — special characters cannot appear consecutively")]
    public void GeneratePassword_ShouldNotContainConsecutiveSpecialCharacters()
    {
        var generator = new PasswordGenerator(new PasswordPolicy());

        for (int i = 0; i < 100; i++)
        {
            string password = generator.GeneratePassword();
            for (int j = 1; j < password.Length; j++)
            {
                bool bothSpecial = SpecialCharacters.Contains(password[j - 1])
                                && SpecialCharacters.Contains(password[j]);
                Assert.False(bothSpecial,
                    $"Consecutive special characters at positions {j - 1}-{j} in: {password}");
            }
        }
    }

    [Fact(Skip = "TODO: Rule 4 — no duplicate numeric digits")]
    public void GeneratePassword_ShouldNotContainDuplicateDigits()
    {
        var generator = new PasswordGenerator(new PasswordPolicy());

        for (int i = 0; i < 100; i++)
        {
            string password = generator.GeneratePassword();
            var digits = password.Where(char.IsDigit).ToList();
            Assert.Equal(digits.Count, digits.Distinct().Count());
        }
    }

    [Fact(Skip = "TODO: Rule 5 — no duplicate special characters")]
    public void GeneratePassword_ShouldNotContainDuplicateSpecialCharacters()
    {
        var generator = new PasswordGenerator(new PasswordPolicy());

        for (int i = 0; i < 100; i++)
        {
            string password = generator.GeneratePassword();
            var specials = password.Where(c => SpecialCharacters.Contains(c)).ToList();
            Assert.Equal(specials.Count, specials.Distinct().Count());
        }
    }

    [Fact(Skip = "TODO: Rules 4+5 — long passwords must still satisfy no-duplicate digit/special rules")]
    public void GeneratePassword_WithLongLength_ShouldRespectDigitAndSpecialLimits()
    {
        // With only 10 digits and 14 special chars, a 64-char password
        // cannot have more than 10 digits or 14 specials if uniqueness is enforced.
        var policy = new PasswordPolicy { MinimumLength = 64, MaximumLength = 64 };
        var generator = new PasswordGenerator(policy);

        string password = generator.GeneratePassword();

        var digitCount = password.Count(char.IsDigit);
        var specialCount = password.Count(c => SpecialCharacters.Contains(c));
        Assert.True(digitCount <= 10,
            $"Password has {digitCount} digits but only 10 unique digits exist: {password}");
        Assert.True(specialCount <= 14,
            $"Password has {specialCount} specials but only 14 unique specials exist: {password}");
    }

    // ---------------------------------------------------------------
    // TODO: MaximumLength enforcement (TODO.md)
    // ---------------------------------------------------------------

    [Fact]
    public void GeneratePassword_CurrentlyIgnoresMaximumLength_UsesMinimumLength()
    {
        // Documents current behavior: MaximumLength is not used.
        // When MaximumLength enforcement is added, this test should
        // be replaced by GeneratePassword_ShouldRespectMaximumLength.
        var policy = new PasswordPolicy { MinimumLength = 16, MaximumLength = 32 };
        var generator = new PasswordGenerator(policy);

        string password = generator.GeneratePassword();

        // Currently always produces MinimumLength, ignoring MaximumLength
        Assert.Equal(16, password.Length);
    }

    [Fact(Skip = "TODO: MaximumLength should be enforced — length should vary between min and max")]
    public void GeneratePassword_ShouldRespectMaximumLength()
    {
        var policy = new PasswordPolicy { MinimumLength = 12, MaximumLength = 32 };
        var generator = new PasswordGenerator(policy);

        var lengths = Enumerable.Range(0, 100)
            .Select(_ => generator.GeneratePassword().Length)
            .ToList();

        Assert.All(lengths, len =>
        {
            Assert.InRange(len, 12, 32);
        });

        // Should produce some variety in length, not always the same
        Assert.True(lengths.Distinct().Count() > 1,
            "Expected varying password lengths between MinimumLength and MaximumLength");
    }

    // ---------------------------------------------------------------
    // TODO: AvoidCommonPatterns (TODO.md)
    // ---------------------------------------------------------------

    [Fact]
    public void GeneratePassword_CurrentlyIgnoresAvoidCommonPatterns()
    {
        // Documents current behavior: AvoidCommonPatterns flag is a no-op.
        // This test ensures the generator still works regardless of the flag value.
        var policyOn = new PasswordPolicy { AvoidCommonPatterns = true };
        var policyOff = new PasswordPolicy { AvoidCommonPatterns = false };

        var genOn = new PasswordGenerator(policyOn);
        var genOff = new PasswordGenerator(policyOff);

        // Both should produce valid passwords without errors
        string pwOn = genOn.GeneratePassword();
        string pwOff = genOff.GeneratePassword();

        Assert.Equal(16, pwOn.Length);
        Assert.Equal(16, pwOff.Length);
    }

    // ---------------------------------------------------------------
    // TODO: Require* flags should control character inclusion (TODO.md)
    // These skipped tests define the correct future behavior.
    // The existing "Ignores*" tests above document current behavior.
    // ---------------------------------------------------------------

    [Fact(Skip = "TODO: RequireUppercase=false should exclude uppercase")]
    public void GeneratePassword_WithRequireUppercaseFalse_ShouldExcludeUppercase()
    {
        var policy = new PasswordPolicy { RequireUppercase = false };
        var generator = new PasswordGenerator(policy);

        for (int i = 0; i < 50; i++)
        {
            string password = generator.GeneratePassword();
            Assert.DoesNotContain(password, c => char.IsUpper(c));
        }
    }

    [Fact(Skip = "TODO: RequireLowercase=false should exclude lowercase")]
    public void GeneratePassword_WithRequireLowercaseFalse_ShouldExcludeLowercase()
    {
        var policy = new PasswordPolicy { RequireLowercase = false };
        var generator = new PasswordGenerator(policy);

        for (int i = 0; i < 50; i++)
        {
            string password = generator.GeneratePassword();
            Assert.DoesNotContain(password, c => char.IsLower(c));
        }
    }

    [Fact(Skip = "TODO: RequireNumber=false should exclude digits")]
    public void GeneratePassword_WithRequireNumberFalse_ShouldExcludeDigits()
    {
        var policy = new PasswordPolicy { RequireNumber = false };
        var generator = new PasswordGenerator(policy);

        for (int i = 0; i < 50; i++)
        {
            string password = generator.GeneratePassword();
            Assert.DoesNotContain(password, c => char.IsDigit(c));
        }
    }

    [Fact(Skip = "TODO: RequireSpecialCharacter=false should exclude specials")]
    public void GeneratePassword_WithRequireSpecialCharacterFalse_ShouldExcludeSpecials()
    {
        var policy = new PasswordPolicy { RequireSpecialCharacter = false };
        var generator = new PasswordGenerator(policy);

        for (int i = 0; i < 50; i++)
        {
            string password = generator.GeneratePassword();
            Assert.DoesNotContain(password, c => SpecialCharacters.Contains(c));
        }
    }

    [Fact(Skip = "TODO: Only letters when numbers and specials are disabled")]
    public void GeneratePassword_WithOnlyLettersRequired_ShouldContainOnlyLetters()
    {
        var policy = new PasswordPolicy
        {
            RequireUppercase = true,
            RequireLowercase = true,
            RequireNumber = false,
            RequireSpecialCharacter = false
        };
        var generator = new PasswordGenerator(policy);

        for (int i = 0; i < 50; i++)
        {
            string password = generator.GeneratePassword();
            Assert.All(password.ToCharArray(), c => Assert.True(char.IsLetter(c),
                $"Expected only letters but found '{c}' in: {password}"));
        }
    }

    [Fact(Skip = "TODO: Single character type — only digits")]
    public void GeneratePassword_WithOnlyDigitsRequired_ShouldContainOnlyDigits()
    {
        var policy = new PasswordPolicy
        {
            RequireUppercase = false,
            RequireLowercase = false,
            RequireNumber = true,
            RequireSpecialCharacter = false
        };
        var generator = new PasswordGenerator(policy);

        string password = generator.GeneratePassword();

        Assert.All(password.ToCharArray(), c => Assert.True(char.IsDigit(c),
            $"Expected only digits but found '{c}' in: {password}"));
    }

    [Fact(Skip = "TODO: Single character type — only specials")]
    public void GeneratePassword_WithOnlySpecialsRequired_ShouldContainOnlySpecials()
    {
        var policy = new PasswordPolicy
        {
            RequireUppercase = false,
            RequireLowercase = false,
            RequireNumber = false,
            RequireSpecialCharacter = true
        };
        var generator = new PasswordGenerator(policy);

        string password = generator.GeneratePassword();

        Assert.All(password.ToCharArray(), c => Assert.True(SpecialCharacters.Contains(c),
            $"Expected only specials but found '{c}' in: {password}"));
    }

    // ---------------------------------------------------------------
    // TODO: Shuffle bias fix (TODO.md — Fisher-Yates)
    // ---------------------------------------------------------------

    [Fact]
    public void GeneratePassword_ShuffleDistribution_EachPositionGetsVariousCharacterTypes()
    {
        // Statistical test: across many generations, each position in the
        // password should see multiple character types, not be biased to one.
        // This test passes with the current OrderBy shuffle but will help
        // detect regressions if the shuffle is replaced with Fisher-Yates.
        var generator = new PasswordGenerator(new PasswordPolicy());

        // Track how many distinct character types appear at each position
        var positionTypes = new HashSet<string>[16];
        for (int p = 0; p < 16; p++)
            positionTypes[p] = new HashSet<string>();

        for (int i = 0; i < 200; i++)
        {
            string password = generator.GeneratePassword();
            for (int p = 0; p < password.Length; p++)
            {
                char c = password[p];
                if (char.IsUpper(c)) positionTypes[p].Add("upper");
                else if (char.IsLower(c)) positionTypes[p].Add("lower");
                else if (char.IsDigit(c)) positionTypes[p].Add("digit");
                else positionTypes[p].Add("special");
            }
        }

        // Every position should have seen at least 2 different character types
        for (int p = 0; p < 16; p++)
        {
            Assert.True(positionTypes[p].Count >= 2,
                $"Position {p} only saw character types: [{string.Join(", ", positionTypes[p])}]");
        }
    }

    // ---------------------------------------------------------------
    // Additional edge cases and safety-net tests
    // ---------------------------------------------------------------

    [Fact]
    public void GeneratePassword_MinimumLengthExceedsMaximumLength_CurrentlyUsesMinimumLength()
    {
        // Documents current behavior: generator uses MinimumLength only.
        // When validation is added, this should throw instead.
        var policy = new PasswordPolicy { MinimumLength = 20, MaximumLength = 12 };
        var generator = new PasswordGenerator(policy);

        string password = generator.GeneratePassword();

        Assert.Equal(20, password.Length);
    }

    [Fact(Skip = "TODO: Should throw when MinimumLength > MaximumLength")]
    public void GeneratePassword_ShouldThrow_WhenMinimumLengthExceedsMaximumLength()
    {
        var policy = new PasswordPolicy { MinimumLength = 20, MaximumLength = 12 };
        var generator = new PasswordGenerator(policy);

        Assert.Throws<ArgumentException>(() => generator.GeneratePassword());
    }

    [Fact]
    public void GeneratePassword_MultipleInstances_ProduceDifferentPasswords()
    {
        var pw1 = new PasswordGenerator(new PasswordPolicy()).GeneratePassword();
        var pw2 = new PasswordGenerator(new PasswordPolicy()).GeneratePassword();

        Assert.NotEqual(pw1, pw2);
    }

    [Fact]
    public void GeneratePassword_PasswordContainsMixOfCharacterTypes_NotDominatedByOne()
    {
        var generator = new PasswordGenerator(new PasswordPolicy());

        for (int i = 0; i < 20; i++)
        {
            string password = generator.GeneratePassword();
            int upperCount = password.Count(char.IsUpper);
            int lowerCount = password.Count(char.IsLower);
            int digitCount = password.Count(char.IsDigit);
            int specialCount = password.Count(c => SpecialCharacters.Contains(c));

            // No single type should consume the entire password
            Assert.True(upperCount < password.Length,
                $"All characters are uppercase in: {password}");
            Assert.True(lowerCount < password.Length,
                $"All characters are lowercase in: {password}");
            Assert.True(digitCount < password.Length,
                $"All characters are digits in: {password}");
            Assert.True(specialCount < password.Length,
                $"All characters are specials in: {password}");
        }
    }

    [Fact]
    public void GeneratePassword_ExactlyMinimumPciLength_StillGuaranteesAllTypes()
    {
        // At length 12 with 4 guaranteed type chars + 8 random fill,
        // all types must be present. Validates across many runs.
        var policy = new PasswordPolicy { MinimumLength = 12, MaximumLength = 12 };
        var generator = new PasswordGenerator(policy);

        for (int i = 0; i < 50; i++)
        {
            string password = generator.GeneratePassword();
            Assert.Equal(12, password.Length);
            Assert.True(password.Any(char.IsUpper), $"Missing uppercase: {password}");
            Assert.True(password.Any(char.IsLower), $"Missing lowercase: {password}");
            Assert.True(password.Any(char.IsDigit), $"Missing digit: {password}");
            Assert.True(password.Any(c => SpecialCharacters.Contains(c)), $"Missing special: {password}");
        }
    }

    [Theory]
    [InlineData(12)]
    [InlineData(16)]
    [InlineData(24)]
    [InlineData(32)]
    [InlineData(48)]
    [InlineData(64)]
    [InlineData(128)]
    public void GeneratePassword_VariousLengths_ProducesCorrectLengthAndValidChars(int length)
    {
        var policy = new PasswordPolicy { MinimumLength = length, MaximumLength = length };
        var generator = new PasswordGenerator(policy);

        string password = generator.GeneratePassword();

        Assert.Equal(length, password.Length);
        Assert.All(password.ToCharArray(), c =>
        {
            bool isValid = char.IsUpper(c) || char.IsLower(c)
                        || char.IsDigit(c) || SpecialCharacters.Contains(c);
            Assert.True(isValid, $"Invalid character '{c}' in password of length {length}");
        });
    }
}

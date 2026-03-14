using SecurePasswordGenerator.Models;

namespace SecurePasswordGenerator.Tests;

/// <summary>
/// Tests for PasswordPolicy model.
/// Tests marked [Fact(Skip = "TODO: ...")] validate behavior planned in TODO.md.
/// Remove the Skip once the corresponding feature is implemented.
/// </summary>
public class PasswordPolicyTests
{
    [Fact]
    public void DefaultPolicy_HasExpectedDefaults()
    {
        var policy = new PasswordPolicy();

        Assert.Equal(16, policy.MinimumLength);
        Assert.Equal(16, policy.MaximumLength);
        Assert.True(policy.RequireUppercase);
        Assert.True(policy.RequireLowercase);
        Assert.True(policy.RequireNumber);
        Assert.True(policy.RequireSpecialCharacter);
        Assert.True(policy.AvoidCommonPatterns);
    }

    [Fact]
    public void Policy_PropertiesCanBeCustomized()
    {
        var policy = new PasswordPolicy
        {
            MinimumLength = 20,
            MaximumLength = 32,
            RequireUppercase = false,
            RequireLowercase = true,
            RequireNumber = false,
            RequireSpecialCharacter = false,
            AvoidCommonPatterns = false
        };

        Assert.Equal(20, policy.MinimumLength);
        Assert.Equal(32, policy.MaximumLength);
        Assert.False(policy.RequireUppercase);
        Assert.True(policy.RequireLowercase);
        Assert.False(policy.RequireNumber);
        Assert.False(policy.RequireSpecialCharacter);
        Assert.False(policy.AvoidCommonPatterns);
    }

    [Fact]
    public void Policy_AllowsZeroMinimumLength()
    {
        var policy = new PasswordPolicy { MinimumLength = 0 };

        Assert.Equal(0, policy.MinimumLength);
    }

    [Fact]
    public void Policy_AllowsNegativeMinimumLength()
    {
        var policy = new PasswordPolicy { MinimumLength = -1 };

        Assert.Equal(-1, policy.MinimumLength);
    }

    [Fact]
    public void Policy_AllowsMinimumLengthGreaterThanMaximumLength()
    {
        var policy = new PasswordPolicy { MinimumLength = 32, MaximumLength = 16 };

        Assert.Equal(32, policy.MinimumLength);
        Assert.Equal(16, policy.MaximumLength);
    }

    [Fact]
    public void Policy_AllowsVeryLargeLength()
    {
        var policy = new PasswordPolicy { MinimumLength = 10_000, MaximumLength = 10_000 };

        Assert.Equal(10_000, policy.MinimumLength);
        Assert.Equal(10_000, policy.MaximumLength);
    }

    // ---------------------------------------------------------------
    // TODO: Policy validation at construction time
    // These tests are skipped until PasswordPolicy validates its own
    // state (TODO.md: "Validate PasswordPolicy at construction time").
    // ---------------------------------------------------------------

    [Fact(Skip = "TODO: Policy should reject MinimumLength > MaximumLength")]
    public void Policy_ShouldThrow_WhenMinimumLengthExceedsMaximumLength()
    {
        Assert.Throws<ArgumentException>(() => new PasswordPolicy
        {
            MinimumLength = 32,
            MaximumLength = 16
        });
    }

    [Fact(Skip = "TODO: Policy should reject negative lengths")]
    public void Policy_ShouldThrow_WhenMinimumLengthIsNegative()
    {
        Assert.Throws<ArgumentException>(() => new PasswordPolicy
        {
            MinimumLength = -1
        });
    }

    [Fact(Skip = "TODO: Policy should reject negative lengths")]
    public void Policy_ShouldThrow_WhenMaximumLengthIsNegative()
    {
        Assert.Throws<ArgumentException>(() => new PasswordPolicy
        {
            MaximumLength = -1
        });
    }

    [Fact(Skip = "TODO: Policy should reject MinimumLength < 12 at construction")]
    public void Policy_ShouldThrow_WhenMinimumLengthBelowPciMinimum()
    {
        Assert.Throws<ArgumentException>(() => new PasswordPolicy
        {
            MinimumLength = 8
        });
    }

    [Fact(Skip = "TODO: Policy should reject length too short for required types")]
    public void Policy_ShouldThrow_WhenMinimumLengthTooShortForRequiredTypes()
    {
        // If all 4 types are required, minimum length must be at least 4
        // Combined with PCI-DSS this means >= 12, but the principle
        // should hold independently of the PCI check.
        Assert.Throws<ArgumentException>(() => new PasswordPolicy
        {
            MinimumLength = 12,
            MaximumLength = 12,
            RequireUppercase = true,
            RequireLowercase = true,
            RequireNumber = true,
            RequireSpecialCharacter = true
        });
    }

    [Fact(Skip = "TODO: Policy should be immutable after construction")]
    public void Policy_ShouldNotAllowMutationAfterConstruction()
    {
        var policy = new PasswordPolicy();

        // Once init-only setters are used, this assignment should
        // produce a compile error. Until then, this test documents
        // that mutation is currently possible.
        policy.MinimumLength = 20;

        // If immutability is enforced, we would never reach this line.
        Assert.Equal(20, policy.MinimumLength);
    }

    [Fact]
    public void Policy_CanSetAllFlagsToFalse()
    {
        var policy = new PasswordPolicy
        {
            RequireUppercase = false,
            RequireLowercase = false,
            RequireNumber = false,
            RequireSpecialCharacter = false,
            AvoidCommonPatterns = false
        };

        Assert.False(policy.RequireUppercase);
        Assert.False(policy.RequireLowercase);
        Assert.False(policy.RequireNumber);
        Assert.False(policy.RequireSpecialCharacter);
        Assert.False(policy.AvoidCommonPatterns);
    }
}

# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run Commands

```bash
dotnet build                    # Build the project
dotnet run                      # Build and run
dotnet build -c Release         # Release build
```

## Test Commands

```bash
dotnet test                                # Run all xUnit tests
dotnet test --verbosity normal             # Run tests with detailed output
dotnet test --filter "ClassName=PasswordGeneratorTests"   # Run a single test class
dotnet test --filter "DisplayName~Shuffle"               # Run tests matching a keyword
```

## Architecture

This is a .NET 9.0 console application that generates secure passwords using cryptographic randomness (`System.Security.Cryptography.RandomNumberGenerator`).

**Application (SecurePasswordGenerator):**

- **Models/** — `PasswordPolicy`: configurable policy (length bounds, character requirements, pattern avoidance). Defaults enforce PCI-DSS compliance (minimum 12 chars).
- **Utilities/** — `RandomCharacterGenerator`: static helper using `RandomNumberGenerator.GetInt32()` for cryptographically secure character generation (uppercase, lowercase, digits, special chars `!@#$%^&*()-_=+`).
- **Services/** — `PasswordGenerator`: takes a `PasswordPolicy`, builds a password by guaranteeing one of each required character type, then fills to minimum length and shuffles. Has TODO rules in comments (no consecutive duplicates, no repeated digits/specials, must start with letter).
- **Commands/** — `GenerateCommand`: Spectre.Console.Cli command that wires up the policy and generator.

`Program.cs` is the entry point — creates a default policy and prints one generated password.

**Tests (SecurePasswordGenerator.Tests):**

xUnit test project with three test classes mirroring the application layers:

- **PasswordPolicyTests** — default values, custom configuration, edge-case inputs (zero, negative, min > max). Includes skipped tests for future policy validation.
- **RandomCharacterGeneratorTests** — type correctness, ASCII range boundaries, variety, and full-range coverage for all four character pools.
- **PasswordGeneratorTests** — happy-path generation, PCI-DSS boundary enforcement, character-type guarantees, uniqueness, shuffle distribution, and valid-character-only checks. Includes skipped tests pre-written for TODO.md items (five generation rules, MaximumLength enforcement, Require* flag respect).

Skipped tests use `[Fact(Skip = "TODO: ...")]`. Remove the `Skip` parameter to activate them once the corresponding feature is implemented. Tests documenting current behavior that will change (e.g. `CurrentlyIgnoresMaximumLength`, `IgnoresRequire*False`) are active and will intentionally break when TODO items are addressed, signalling that they need updating.

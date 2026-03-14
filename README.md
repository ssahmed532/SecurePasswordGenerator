# SecurePasswordGenerator

A .NET 9.0 console application that generates secure passwords using cryptographic randomness (`System.Security.Cryptography.RandomNumberGenerator`).

## Features

- Cryptographically secure random character generation
- Configurable password policy with PCI-DSS compliant defaults (minimum 12 characters)
- Guarantees inclusion of uppercase, lowercase, digits, and special characters
- Shuffle-based generation to avoid predictable patterns

## Project Structure

```
├── Commands/
│   └── GenerateCommand.cs           # Spectre.Console.Cli command
├── Models/
│   └── PasswordPolicy.cs            # Configurable policy (length, character requirements)
├── Services/
│   └── PasswordGenerator.cs         # Password generation logic
├── Utilities/
│   └── RandomCharacterGenerator.cs  # Cryptographically secure character generation
├── Program.cs                       # Entry point
│
└── SecurePasswordGenerator.Tests/   # xUnit test project
    ├── PasswordPolicyTests.cs       # Policy model tests
    ├── PasswordGeneratorTests.cs    # Generation logic tests
    └── RandomCharacterGeneratorTests.cs  # Character generator tests
```

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

## Build & Run

```bash
dotnet build              # Build the project
dotnet run                # Build and run
dotnet build -c Release   # Release build
```

## Usage

Run the application to generate a secure password:

```
$ dotnet run
Generated Secure Password:
k7@Lm2!xRp9#Nq
```

## Testing

The project includes an xUnit test suite covering all three application layers. Tests are in the `SecurePasswordGenerator.Tests/` project.

```bash
# Run all tests
dotnet test

# Run tests with detailed per-test output
dotnet test --verbosity normal

# Run a single test class
dotnet test --filter "ClassName=PasswordGeneratorTests"

# Run tests matching a keyword (e.g. all shuffle-related tests)
dotnet test --filter "DisplayName~Shuffle"

# Run only non-skipped tests (default behavior — skipped tests are excluded automatically)
dotnet test
```

### Test categories

| Test class | Covers | Active | Skipped |
|---|---|---|---|
| `PasswordPolicyTests` | Default values, custom configuration, edge cases | 7 | 6 |
| `RandomCharacterGeneratorTests` | Type correctness, ASCII ranges, variety, full-range coverage | 15 | 0 |
| `PasswordGeneratorTests` | Happy path, PCI-DSS boundaries, character guarantees, uniqueness, shuffle quality | 42 | 15 |

**Skipped tests** are pre-written for features planned in `TODO.md` (generation rules, `MaximumLength` enforcement, `Require*` flag respect, policy validation). They use `[Fact(Skip = "TODO: ...")]` and are ready to activate by removing the `Skip` parameter once the corresponding feature is implemented.

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.

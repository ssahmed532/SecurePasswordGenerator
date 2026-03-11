# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run Commands

```bash
dotnet build                    # Build the project
dotnet run                      # Build and run
dotnet build -c Release         # Release build
```

There are no tests in this project currently.

## Architecture

This is a .NET 9.0 console application that generates secure passwords using cryptographic randomness (`System.Security.Cryptography.RandomNumberGenerator`).

**Three-layer structure:**

- **Models/** — `PasswordPolicy`: configurable policy (length bounds, character requirements, pattern avoidance). Defaults enforce PCI-DSS compliance (minimum 12 chars).
- **Utilities/** — `RandomCharacterGenerator`: static helper using `RandomNumberGenerator.GetInt32()` for cryptographically secure character generation (uppercase, lowercase, digits, special chars `!@#$%^&*()-_=+`).
- **Services/** — `PasswordGenerator`: takes a `PasswordPolicy`, builds a password by guaranteeing one of each required character type, then fills to minimum length and shuffles. Has TODO rules in comments (no consecutive duplicates, no repeated digits/specials, must start with letter).

`Program.cs` is the entry point — creates a default policy and prints one generated password.

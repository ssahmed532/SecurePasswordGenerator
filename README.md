# SecurePasswordGenerator

A .NET 9.0 console application that generates secure passwords using cryptographic randomness (`System.Security.Cryptography.RandomNumberGenerator`).

## Features

- Cryptographically secure random character generation
- Configurable password policy with PCI-DSS compliant defaults (minimum 12 characters)
- Guarantees inclusion of uppercase, lowercase, digits, and special characters
- Shuffle-based generation to avoid predictable patterns

## Project Structure

```
├── Models/
│   └── PasswordPolicy.cs          # Configurable policy (length, character requirements)
├── Services/
│   └── PasswordGenerator.cs       # Password generation logic
├── Utilities/
│   └── RandomCharacterGenerator.cs # Cryptographically secure character generation
└── Program.cs                     # Entry point
```

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

## Build & Run

```bash
dotnet build          # Build the project
dotnet run            # Build and run
dotnet build -c Release   # Release build
```

## Usage

Run the application to generate a secure password:

```
$ dotnet run
Generated Secure Password:
k7@Lm2!xRp9#Nq
```

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.

# TODO — SecurePasswordGenerator Enhancements

## Framework Upgrade

- [ ] **Upgrade from .NET 9.0 to .NET 10.** Update `TargetFramework` in `SecurePasswordGenerator.csproj` from `net9.0` to `net10.0`, update the solution file if needed, verify all dependencies are compatible, and take advantage of any new language features or performance improvements.

## Bug Fixes & Existing Rule Implementation

- [ ] **Implement the five generation rules documented in `PasswordGenerator.GeneratePassword()`:**
  1. Password must start with an uppercase or lowercase character
  2. No consecutive duplicate characters
  3. Special characters cannot appear consecutively
  4. A numeric digit cannot appear more than once in the password
  5. A special character cannot appear more than once in the password
  - The current shuffle (`OrderBy(… GetInt32 …)`) undoes any ordering guarantees — the generation logic needs to enforce these constraints *after* the final shuffle, or replace the shuffle with a constrained placement strategy.

- [ ] **`MaximumLength` is never used.** `GeneratePassword()` fills to `MinimumLength` but never caps at `MaximumLength`. Passwords should have a random length between min and max.

- [ ] **`AvoidCommonPatterns` flag is declared but never checked.** Decide what patterns to reject (dictionary words, keyboard walks like "qwerty", date-like sequences) and implement the check.

- [ ] **Policy requirements are ignored during generation.** If `RequireUppercase` is `false`, the generator still always adds an uppercase character. Each `Require*` flag should control whether that character type is included.

- [ ] **Shuffle bias.** `OrderBy(_ => RandomNumberGenerator.GetInt32(0, 100))` can produce ties, causing the sort to be non-uniform. Use Fisher-Yates (Knuth) shuffle for an unbiased permutation.

## Code Cleanup

- [ ] **Remove unused `using` statements.** `Program.cs` imports `System` but doesn't use it directly (implicit usings are enabled in the project). Same issue in `RandomCharacterGenerator.cs`.

- [ ] **Replace magic numbers with character literals in `RandomCharacterGenerator.cs`.** ASCII ranges `(65, 91)`, `(97, 123)`, `(48, 58)` should be expressed as `('A', 'Z' + 1)`, `('a', 'z' + 1)`, `('0', '9' + 1)` for readability.

- [ ] **Adopt consistent namespace style.** `Program.cs` has no namespace while other files use block-scoped namespaces. Standardise on file-scoped namespaces throughout for consistency and reduced nesting.

## Refactoring

- [ ] **Break up `GeneratePassword()` into focused private methods.** It currently validates the policy, guarantees required character types, fills remaining length, and shuffles — all in one method. Extract each responsibility (e.g., `EnsureRequiredCharacters()`, `FillToLength()`, `ShufflePassword()`) for clarity and testability.

- [ ] **Extract the shuffle into a reusable utility method.** The inline `OrderBy` shuffle in `GeneratePassword()` should be a standalone `Shuffle<T>()` method using Fisher-Yates, usable elsewhere and independently testable.

- [ ] **Make `RandomCharacterGenerator` an instance with an injectable character pool.** It is currently a static class with a hardcoded special character set. Converting it to an instance class that accepts allowed characters via its constructor would support customisable character pools and make it testable with deterministic fakes.

- [ ] **Improve character-type weighting in `GetRandomCharacter()`.** It currently gives equal 25% probability to all four character types regardless of how many characters of each type are already in the password. Consider weighting toward underrepresented types to produce more balanced passwords.

## Code Structure & Design Improvements

- [ ] **Validate `PasswordPolicy` at construction time.** There is no input validation — `MinimumLength` could be 100 while `MaximumLength` is 16, or values could be negative. Move the PCI-DSS minimum check out of `GeneratePassword()` and add a `Validate()` method (or constructor validation) that catches invalid configurations early: `MaximumLength < MinimumLength`, negative values, minimum length too short to satisfy all required character types, etc.

- [ ] **Make `PasswordPolicy` immutable.** Public setters mean the policy can be mutated after the generator is constructed, which could cause unexpected behaviour mid-generation. Switch to `init`-only setters or a constructor with required parameters.

- [ ] **Separate bootstrap/wiring from `Program.cs`.** All configuration is currently inline in `Main()`. As the app grows with CLI arguments and DI, extract a configuration or bootstrap step to keep the entry point clean.

- [ ] **Dependency injection / interface extraction.** Extract `IPasswordGenerator` and `IRandomCharacterGenerator` interfaces so the generator is testable with deterministic fakes and extensible for different generation strategies.

- [ ] **Library packaging.** Separate the core library from the console entry point (e.g., `SecurePasswordGenerator.Core` class library + `SecurePasswordGenerator.Cli` console app) so the generator can be consumed as a NuGet package.

- [ ] **Async-friendly API.** Not critical today, but if breach-checking or file-based word lists are added, having `async` variants of the generation pipeline avoids blocking.

## Testing

- [ ] **Add a test project** (e.g., xUnit) with tests for:
  - Policy validation (reject `MinimumLength < 12`, `MinimumLength > MaximumLength`, etc.)
  - Character-type distribution — generated passwords satisfy every `Require*` flag
  - Constraint rules (no consecutive duplicates, no repeated digits/specials, starts with letter)
  - Edge cases: minimum possible length, all flags disabled except one, special-char pool exhaustion when password is long

## CLI & Usability

- [ ] **Command-line arguments.** Accept options like `--length`, `--count`, `--no-special`, `--exclude-chars`, `--policy-file` so users can customise generation without recompiling. Consider `System.CommandLine` or a simple arg parser.

- [ ] **Generate multiple passwords at once.** Add a `--count N` option and a `GeneratePasswords(int count)` method.

- [ ] **Copy to clipboard.** Optionally copy the generated password to the system clipboard and clear it after a configurable timeout.

- [ ] **Exit codes & stderr.** Return non-zero exit codes on error and write diagnostics to stderr so the tool composes well in scripts.

## Password Strength & Security

- [ ] **Password strength estimator.** Calculate and display entropy bits or integrate an algorithm like zxcvbn to give users feedback on the generated password's resistance to attack.

- [ ] **Configurable character pools.** Allow users to supply a custom set of allowed special characters (some systems restrict which specials are valid) or exclude ambiguous characters (`0O`, `1lI`).

- [ ] **Passphrase mode.** Add an alternative mode that generates passphrases from a word list (e.g., EFF dice-word list), with configurable separator, word count, and optional digit/symbol insertion.

- [ ] **Breach / dictionary check.** Optionally check the generated password's SHA-1 prefix against the Have I Been Pwned Passwords API (k-anonymity model) to confirm it hasn't appeared in known breaches.

## Distribution & Integration

- [ ] **Publish as a .NET global tool** (`dotnet tool install -g`) for easy installation.

- [ ] **GitHub Actions CI.** Add a workflow that builds, runs tests, and publishes releases on tag push.

- [ ] **Cross-platform single-file publish.** Add publish profiles for self-contained single-file executables on Windows, macOS, and Linux.

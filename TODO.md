# TODO — SecurePasswordGenerator Enhancements

Items are grouped by priority. Within each group, tackle items top-to-bottom.
Skipped unit tests in `SecurePasswordGenerator.Tests` are pre-written for many
of these items — remove the `Skip` parameter to activate them as you go.

---

## P0 — Correctness & Security (fix what's broken)

These items are tightly coupled and should ideally be tackled together as a
single body of work, since implementing the generation rules (1) requires
fixing the shuffle (2), and honouring the Require* flags (3) changes which
character types enter the pool.

- [ ] **Implement the five generation rules documented in `PasswordGenerator.GeneratePassword()`:**
  1. Password must start with an uppercase or lowercase character
  2. No consecutive duplicate characters
  3. Special characters cannot appear consecutively
  4. A numeric digit cannot appear more than once in the password
  5. A special character cannot appear more than once in the password
  - The current shuffle (`OrderBy(… GetInt32 …)`) undoes any ordering guarantees — the generation logic needs to enforce these constraints *after* the final shuffle, or replace the shuffle with a constrained placement strategy.
  - **Tests ready to activate:** `GeneratePassword_ShouldStartWithLetter`, `GeneratePassword_ShouldNotContainConsecutiveDuplicates`, `GeneratePassword_ShouldNotContainConsecutiveSpecialCharacters`, `GeneratePassword_ShouldNotContainDuplicateDigits`, `GeneratePassword_ShouldNotContainDuplicateSpecialCharacters`, `GeneratePassword_WithLongLength_ShouldRespectDigitAndSpecialLimits`

- [ ] **Fix shuffle bias.** `OrderBy(_ => RandomNumberGenerator.GetInt32(0, 100))` can produce ties on a 16-char password (~1.2 expected collisions), causing the sort to be non-uniform. Replace with a Fisher-Yates (Knuth) shuffle using `RandomNumberGenerator.GetInt32()` — it's a 10-line method and eliminates the problem entirely.
  - **Existing active test as safety net:** `GeneratePassword_ShuffleDistribution_EachPositionGetsVariousCharacterTypes`

- [ ] **Honour the `Require*` policy flags during generation.** If `RequireUppercase` is `false`, the generator still always adds an uppercase character. Each `Require*` flag should control whether that character type is included.
  - **Tests ready to activate:** `GeneratePassword_WithRequireUppercaseFalse_ShouldExcludeUppercase`, `GeneratePassword_WithRequireLowercaseFalse_ShouldExcludeLowercase`, `GeneratePassword_WithRequireNumberFalse_ShouldExcludeDigits`, `GeneratePassword_WithRequireSpecialCharacterFalse_ShouldExcludeSpecials`, `GeneratePassword_WithOnlyLettersRequired_ShouldContainOnlyLetters`, `GeneratePassword_WithOnlyDigitsRequired_ShouldContainOnlyDigits`, `GeneratePassword_WithOnlySpecialsRequired_ShouldContainOnlySpecials`
  - **Active tests that will intentionally break (update them):** `GeneratePassword_IgnoresRequireUppercaseFalse_StillContainsUppercase`, `GeneratePassword_IgnoresRequireLowercaseFalse_StillContainsLowercase`, `GeneratePassword_IgnoresRequireNumberFalse_StillContainsDigit`, `GeneratePassword_IgnoresRequireSpecialCharacterFalse_StillContainsSpecial`

- [ ] **Enforce `MaximumLength`.** `GeneratePassword()` fills to `MinimumLength` but never caps at `MaximumLength`. Passwords should have a random length in `[MinimumLength, MaximumLength]`.
  - **Test ready to activate:** `GeneratePassword_ShouldRespectMaximumLength`
  - **Active test that will intentionally break:** `GeneratePassword_CurrentlyIgnoresMaximumLength_UsesMinimumLength`

- [ ] **Handle character-pool exhaustion.** With rules 4+5 (unique digits/specials), a 64-char password can have at most 10 digits and 14 specials. The generator needs to gracefully fall back to letters when pools are exhausted, and reject configurations where the requested length is impossible to satisfy given the active constraints.

---

## P1 — Robustness (fail fast, prevent misuse)

- [ ] **Validate `PasswordPolicy` at construction time.** There is no input validation — `MinimumLength` could be 100 while `MaximumLength` is 16, or values could be negative. Move the PCI-DSS minimum check out of `GeneratePassword()` and add a `Validate()` method (or constructor validation) that catches invalid configurations early: `MaximumLength < MinimumLength`, negative values, minimum length too short to satisfy all required character types, etc.
  - **Tests ready to activate:** `Policy_ShouldThrow_WhenMinimumLengthExceedsMaximumLength`, `Policy_ShouldThrow_WhenMinimumLengthIsNegative`, `Policy_ShouldThrow_WhenMaximumLengthIsNegative`, `Policy_ShouldThrow_WhenMinimumLengthBelowPciMinimum`, `Policy_ShouldThrow_WhenMinimumLengthTooShortForRequiredTypes`, `GeneratePassword_ShouldThrow_WhenMinimumLengthExceedsMaximumLength`
  - **Active tests that will intentionally break:** `Policy_AllowsZeroMinimumLength`, `Policy_AllowsNegativeMinimumLength`, `Policy_AllowsMinimumLengthGreaterThanMaximumLength`, `GeneratePassword_MinimumLengthExceedsMaximumLength_CurrentlyUsesMinimumLength`

- [ ] **Make `PasswordPolicy` immutable.** Public setters mean the policy can be mutated after the generator is constructed, which could cause unexpected behaviour mid-generation. Switch to `init`-only setters or a constructor with required parameters.
  - **Test ready to activate:** `Policy_ShouldNotAllowMutationAfterConstruction`

- [ ] **Implement `AvoidCommonPatterns`.** The flag is declared but never checked. Decide what patterns to reject (dictionary words, keyboard walks like "qwerty", date-like sequences) and implement the check.
  - **Active test documenting current no-op:** `GeneratePassword_CurrentlyIgnoresAvoidCommonPatterns`

---

## P2 — Architecture & Refactoring (improve maintainability)

- [ ] **Break up `GeneratePassword()` into focused private methods.** It currently validates the policy, guarantees required character types, fills remaining length, and shuffles — all in one method. Extract each responsibility (e.g., `EnsureRequiredCharacters()`, `FillToLength()`, `ShufflePassword()`) for clarity and testability.

- [ ] **Extract the shuffle into a reusable utility method.** The inline `OrderBy` shuffle in `GeneratePassword()` should be a standalone `Shuffle<T>()` method using Fisher-Yates, usable elsewhere and independently testable.

- [ ] **Make `RandomCharacterGenerator` an instance with an injectable character pool.** It is currently a static class with a hardcoded special character set. Converting it to an instance class that accepts allowed characters via its constructor would support customisable character pools and make it testable with deterministic fakes.

- [ ] **Improve character-type weighting in `GetRandomCharacter()`.** It currently gives equal 25% probability to all four character types regardless of how many characters of each type are already in the password or which types are enabled. Consider weighting toward underrepresented types to produce more balanced passwords, and only selecting from enabled types.

- [ ] **Dependency injection / interface extraction.** Extract `IPasswordGenerator` and `IRandomCharacterGenerator` interfaces so the generator is testable with deterministic fakes and extensible for different generation strategies.

- [ ] **Separate library from CLI.** Split into `SecurePasswordGenerator.Core` (class library) and `SecurePasswordGenerator.Cli` (console app) so the generator can be consumed as a NuGet package independently.

- [ ] **Separate bootstrap/wiring from `Program.cs`.** All configuration is currently inline. As the app grows with CLI arguments and DI, extract a configuration or bootstrap step to keep the entry point clean.

---

## P3 — Code Cleanup (quick wins)

- [ ] **Remove unused `using` statements.** `Program.cs` imports `System` but doesn't use it directly (implicit usings are enabled in the project). Same issue in `RandomCharacterGenerator.cs`.

- [ ] **Replace magic numbers with character literals in `RandomCharacterGenerator.cs`.** ASCII ranges `(65, 91)`, `(97, 123)`, `(48, 58)` should be expressed as `('A', 'Z' + 1)`, `('a', 'z' + 1)`, `('0', '9' + 1)` for readability.

- [ ] **Adopt consistent namespace style.** `Program.cs` has no namespace while other files use block-scoped namespaces. Standardise on file-scoped namespaces throughout for consistency and reduced nesting.

---

## P4 — CLI & Usability (user-facing features)

- [ ] **Command-line arguments.** Accept options like `--length`, `--count`, `--no-special`, `--exclude-chars`, `--policy-file` via `GenerateCommand.Settings` (Spectre.Console.Cli infrastructure is already in place — the Settings class is just empty).

- [ ] **Generate multiple passwords at once.** Add a `--count N` option and a `GeneratePasswords(int count)` method.

- [ ] **Copy to clipboard.** Optionally copy the generated password to the system clipboard and clear it after a configurable timeout.

- [ ] **Exit codes & stderr.** Return non-zero exit codes on error and write diagnostics to stderr so the tool composes well in scripts.

---

## P5 — Password Strength & Security (advanced features)

- [ ] **Password strength estimator.** Calculate and display entropy bits or integrate an algorithm like zxcvbn to give users feedback on the generated password's resistance to attack.

- [ ] **Configurable character pools.** Allow users to supply a custom set of allowed special characters (some systems restrict which specials are valid) or exclude ambiguous characters (`0O`, `1lI`).

- [ ] **Passphrase mode.** Add an alternative mode that generates passphrases from a word list (e.g., EFF dice-word list), with configurable separator, word count, and optional digit/symbol insertion.

- [ ] **Breach / dictionary check.** Optionally check the generated password's SHA-1 prefix against the Have I Been Pwned Passwords API (k-anonymity model) to confirm it hasn't appeared in known breaches.

---

## P6 — Distribution & Integration (release pipeline)

- [ ] **GitHub Actions CI.** Add a workflow that builds, runs tests, and publishes releases on tag push.

- [ ] **Publish as a .NET global tool** (`dotnet tool install -g`) for easy installation.

- [ ] **Cross-platform single-file publish.** Add publish profiles for self-contained single-file executables on Windows, macOS, and Linux.

---

## P7 — Framework Upgrade (when .NET 10 is stable)

- [ ] **Upgrade from .NET 9.0 to .NET 10.** Update `TargetFramework` in both `.csproj` files from `net9.0` to `net10.0`, verify all dependencies are compatible, and take advantage of any new language features or performance improvements.

- [ ] **Async-friendly API.** Not critical today, but if breach-checking or file-based word lists are added, having `async` variants of the generation pipeline avoids blocking.

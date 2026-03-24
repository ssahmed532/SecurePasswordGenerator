# TODO — SecurePasswordGenerator Enhancements

Items are grouped by priority. Within each group, tackle items top-to-bottom.
Skipped unit tests in `SecurePasswordGenerator.Tests` are pre-written for many
of these items — remove the `Skip` parameter to activate them as you go.

---

## Master Findings Summary

Consolidated from: codebase gap analysis, cryptographic security review, competitive feature research (pwgen, Bitwarden CLI, 1Password CLI, keepassxc-cli, gopass, diceware, xkpasswd, apg-go, NSA RandPassGenerator), and repository security audit.

| # | Sev | Category | Location | Finding | Priority |
|---|-----|----------|----------|---------|----------|
| 1 | HIGH | Crypto | `Services/PasswordGenerator.cs:24-25` | Rules 4+5 (unique digits/specials) reduce entropy — digit combinations nearly halved (10^4 → 5,040). NIST SP 800-63B recommends maximising entropy over composition rules. | P0 |
| 2 | HIGH | Crypto | `Services/PasswordGenerator.cs:47-54` | `GetRandomCharacter()` non-uniform: each digit is 2.6× more likely than any letter. Actual entropy ~89 bits vs theoretical ~100 bits. | P0 |
| 3 | HIGH | Testing | `Commands/GenerateCommand.cs` | Zero test coverage for the entire CLI command layer. | P7 |
| 4 | MED | Security | `Services/PasswordGenerator.cs:36` | No upper bound on password length — `int.MaxValue` causes OOM via unbounded `StringBuilder`. | P0 |
| 5 | MED | Security | `Services/PasswordGenerator.cs:30-42` | 3–4 copies of password in managed memory (StringBuilder, ToString, ToCharArray, final string) — none can be zeroed. | P5 |
| 6 | MED | Security | `Commands/GenerateCommand.cs:20-21` | Password printed to stdout via `AnsiConsole.WriteLine` — persists in terminal scrollback, screen recordings, redirected logs. No masking option. | P4 |
| 7 | MED | Security | `SecurePasswordGenerator.csproj` | No NuGet lock file — version numbers treated as minimums (`>=`), risk of silent dependency upgrade. | P6 |
| 8 | MED | Code | `Utilities/RandomCharacterGenerator.cs:9`, `Tests/PasswordGeneratorTests.cs:13`, `Tests/RandomCharacterGeneratorTests.cs:7` | `SpecialCharacters` string `"!@#$%^&*()-_=+"` duplicated in 3 files with no single source of truth. | P2 |
| 9 | MED | Code | `Commands/GenerateCommand.cs:14` | `Execute()` catches no exceptions — invalid policy produces raw stack trace. | P1 |
| 10 | MED | Code | `Program.cs:9` + `SecurePasswordGenerator.csproj:8` | Version `"0.2.1"` hardcoded in two places — will drift. | P2 |
| 11 | MED | Build | Both `.csproj` files | No `Directory.Build.props` — `TargetFramework`, `ImplicitUsings`, `Nullable` duplicated. | P6 |
| 12 | MED | Build | Project root | No `.editorconfig` — inconsistent namespace styles and formatting not enforced. | P8 |
| 13 | MED | Feature | — | No passphrase/diceware mode — most requested alternative in competitive tools (Bitwarden, keepassxc, gopass, diceware). | P5 |
| 14 | MED | Feature | — | No `--quiet` / pipe-friendly mode — label line breaks `spg \| xclip` usage. | P4 |
| 15 | MED | Feature | — | No entropy display — basic expectation for any password generator. | P5 |
| 16 | MED | Feature | — | No `--exclude-ambiguous` flag (`0O1lI`) — standard in pwgen, Bitwarden CLI, keepassxc-cli. | P4 |
| 17 | LOW | Crypto | `Services/PasswordGenerator.cs:42` | `OrderBy` is a stable sort — tied elements preserve original order (uppercase→lowercase→digit→special), leaking placement bias beyond simple collision probability. | P0 |
| 18 | LOW | Code | `Services/PasswordGenerator.cs:28` | `ArgumentException` thrown but invalid value is `_policy` field, not a method parameter. Should be `InvalidOperationException`. | P1 |
| 19 | LOW | Code | `Services/PasswordGenerator.cs:14` | No null guard on constructor — `new PasswordGenerator(null!)` gives unhelpful `NullReferenceException`. | P1 |
| 20 | LOW | Code | `Commands/GenerateCommand.cs:10-12` | No `[Description]` attribute — `--help` shows no tool description. | P3 |
| 21 | LOW | Code | `PasswordGenerator.cs:1-2` | Redundant `using System;` and `using System.Linq;` (covered by implicit usings). Not mentioned in original TODO for this file. | P3 |
| 22 | LOW | Code | `PasswordGenerator.cs:47` | Magic number `4` — should be named constant or derived from enabled type count. | P3 |
| 23 | LOW | Build | Project root | No `global.json` to pin .NET SDK version — different SDKs produce different binaries. | P6 |
| 24 | LOW | Build | `Tests/SecurePasswordGenerator.Tests.csproj:11` | `coverlet.collector` dependency included but no coverage format, threshold, or CI integration configured. Dead weight. | P7 |
| 25 | LOW | Build | Project root | No SBOM generation — important for supply-chain trust in a security tool. | P6 |
| 26 | LOW | .NET | All public classes | No `sealed` keyword — missed JIT devirtualisation opportunity. | P3 |
| 27 | LOW | .NET | All public APIs | No XML `<summary>` doc comments; `<GenerateDocumentationFile>` not set. | P8 |
| 28 | LOW | Testing | `Tests/PasswordGeneratorTests.cs:13`, `Tests/RandomCharacterGeneratorTests.cs:7` | `SpecialCharacters` hardcoded in tests — will silently diverge from source if changed. | P7 |
| 29 | LOW | Hygiene | All 7 commits | Git author metadata exposes real name and personal Gmail address. | P8 |
| 30 | LOW | Hygiene | `.gitignore` | Missing patterns: `*.nupkg`, `*.log`, `TestResults/`, `*.cache`, `*.DotSettings.user`, `publish/`, etc. | P8 |

---

## Entropy Analysis

Current character pool: 26 uppercase + 26 lowercase + 10 digits + 14 specials = **76 characters**.

| Scenario | Entropy (16-char password) | Notes |
|----------|---------------------------|-------|
| Pure uniform random from 76 chars | **99.8 bits** (log₂(76¹⁶)) | Theoretical maximum |
| Current algorithm (25% per type, then per-pool uniform) | **~89–91 bits** | Digits get P=1/4 × 1/10 = 2.5% each; letters get P=1/4 × 1/26 = 0.96% each. Per-char Shannon entropy ≈ 6.05 bits vs theoretical 6.25 bits. |
| After rules 4+5 (no duplicate digits/specials) | **~80–85 bits** | Digit component: 10^4 → 10×9×8×7 = 5,040 (halved). Special component: 14^4 → 14×13×12×11 (reduced ~40%). |
| Letters-only (if specials/digits disabled) | **75.2 bits** (log₂(52¹⁶)) | Fallback if only upper+lower enabled |

**Key recommendation:** Fix `GetRandomCharacter()` to select uniformly from the full pool (P0), and make uniqueness rules 4+5 opt-in rather than unconditional (entropy warning added to P0).

---

## Competitive Feature Landscape

Features implemented by leading CLI password generators, mapped to this project's status.

### Generation Modes

| Feature | pwgen | Bitwarden CLI | keepassxc-cli | gopass | 1Password CLI | xkpasswd | This Project |
|---------|-------|---------------|---------------|-------|---------------|----------|-------------|
| Random character password | Yes | Yes | Yes | Yes | Yes | Yes | **Yes** (current) |
| Passphrase / diceware | — | Yes | Yes | Yes | — | Yes | TODO (P5) |
| Pronounceable passwords | Yes | — | — | — | — | — | Not planned |
| PIN / numeric-only | — | Yes | — | — | — | — | TODO (P5) |
| Custom pattern template | — | — | — | — | — | Yes | Not planned |
| Base64 / hex / raw key | — | — | — | — | — | — | Not planned |
| Multiple at once (`--count N`) | Yes | Yes | Yes | Yes | — | Yes | TODO (P4) |

### Output & Formatting

| Feature | pwgen | Bitwarden CLI | keepassxc-cli | gopass | diceware | This Project |
|---------|-------|---------------|---------------|-------|----------|-------------|
| TTY vs pipe auto-detection | Yes | — | — | — | — | TODO (P4) |
| JSON output mode | — | — | — | — | — | TODO (P4) |
| Quiet / plain mode | — | — | — | — | Yes | TODO (P4) |
| No-colour flag | — | — | — | — | Yes | TODO (P4) |
| Copy to clipboard | — | — | Yes | Yes | — | TODO (P4) |
| Clipboard auto-clear (timeout) | — | — | Yes | Yes | — | TODO (P4) |
| Color-coded character types | — | — | — | — | — | TODO (P4) |
| Screen masking / reveal toggle | — | — | — | — | — | TODO (P4) |

### Policy & Compliance

| Feature | pwgen | Bitwarden CLI | keepassxc-cli | gopass | NSA RandPassGen | This Project |
|---------|-------|---------------|---------------|-------|-----------------|-------------|
| Min / max length | Yes | Yes | Yes | Yes | Yes | **Yes** (MaxLength unenforced — P0) |
| Required character classes | — | Yes | Yes | Yes | — | **Partial** (flags exist, ignored — P0) |
| Min count per class (`--min-number`) | — | Yes | — | — | — | TODO (P4) |
| Exclude specific characters | — | — | Yes | — | Yes | TODO (P4) |
| Exclude ambiguous (`0O1lI`) | Yes | Yes | Yes | — | — | TODO (P4) |
| No-vowels (avoid offensive words) | Yes | — | — | — | — | TODO (P5) |
| Compliance presets (PCI, NIST) | — | — | — | — | — | TODO (P4) |
| Policy from config file | — | — | — | — | — | TODO (P4) |

### Security Hardening

| Feature | pwgen | keepassxc-cli | gopass | NSA RandPassGen | This Project |
|---------|-------|---------------|-------|-----------------|-------------|
| Cryptographic RNG | Yes | Yes | Yes | Yes | **Yes** |
| Fisher-Yates shuffle | Best practice | Assumed | Assumed | Assumed | TODO (P0) |
| Clipboard auto-clear | — | Yes (45s) | Yes (45s) | — | TODO (P4) |
| Screen-clear after display | Yes | — | — | — | TODO (P4) |
| Memory zeroing / secure buffers | — | — | — | Yes (NIST DRBG) | TODO (P5) |
| Entropy source documentation | — | — | — | Yes | Not planned |

### Validation & Feedback

| Feature | zxcvbn-cli | Bitwarden CLI | sebastienrousseau/pwgen | This Project |
|---------|-----------|---------------|-------------------------|-------------|
| Entropy bits display | Yes | — | Yes | TODO (P5) |
| Crack time estimation | Yes | — | — | TODO (P5) |
| Strength score (0–4) | Yes | — | — | TODO (P5) |
| HIBP breach check | Various tools | — | — | TODO (P5) |
| Character class breakdown | — | — | Yes | TODO (P4, via `--verbose`) |

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
  - **Location:** `Services/PasswordGenerator.cs:20-25` (comments documenting the rules)
  - The current shuffle (`OrderBy(… GetInt32 …)`) undoes any ordering guarantees — the generation logic needs to enforce these constraints *after* the final shuffle, or replace the shuffle with a constrained placement strategy.
  - **Tests ready to activate:** `GeneratePassword_ShouldStartWithLetter`, `GeneratePassword_ShouldNotContainConsecutiveDuplicates`, `GeneratePassword_ShouldNotContainConsecutiveSpecialCharacters`, `GeneratePassword_ShouldNotContainDuplicateDigits`, `GeneratePassword_ShouldNotContainDuplicateSpecialCharacters`, `GeneratePassword_WithLongLength_ShouldRespectDigitAndSpecialLimits`
  - **⚠ Entropy warning (HIGH):** Rules 4+5 (unique digits/specials) significantly reduce the password's keyspace. For a 16-char password with ~4 digits, the digit component drops from 10⁴ = 10,000 combinations to 10×9×8×7 = 5,040 — nearly halving it. Similarly for specials: 14⁴ → 14×13×12×11 (~40% reduction). Overall entropy drops from ~89 bits to ~80–85 bits. Consider making these rules **opt-in via policy flags** rather than unconditional, and document the entropy trade-off. NIST SP 800-63B recommends maximising entropy over composition rules. See [Entropy Analysis](#entropy-analysis) table above.
  - **⚠ Side-channel note (LOW):** If rejection sampling is used to enforce uniqueness rules, variable iteration counts could theoretically reveal password composition via timing. Not exploitable for a local CLI tool, but relevant if the generator is ever exposed as a network service.

- [ ] **Fix shuffle bias.** `OrderBy(_ => RandomNumberGenerator.GetInt32(0, 100))` can produce ties on a 16-char password (~1.2 expected collisions), causing the sort to be non-uniform. Replace with a Fisher-Yates (Knuth) shuffle using `RandomNumberGenerator.GetInt32()` — it's a 10-line method and eliminates the problem entirely.
  - **Location:** `Services/PasswordGenerator.cs:42`
  - **Stable-sort nuance:** .NET's `OrderBy` is a *stable* sort, so tied elements preserve their original order. Since the first four characters are always uppercase → lowercase → digit → special (lines 31–34 of `PasswordGenerator.cs`), ties cause these seed characters to retain their relative ordering more often than chance. This is a subtle but real non-uniformity beyond simple collision probability — an attacker knowing this bias could narrow the search space for the first few positions.
  - **Existing active test as safety net:** `GeneratePassword_ShuffleDistribution_EachPositionGetsVariousCharacterTypes`

- [ ] **Fix non-uniform character distribution in `GetRandomCharacter()`.** The method gives each of the 4 character types a flat 25% probability, but the pools have different sizes (26, 26, 10, 14 = 76 total). This creates measurable bias:
  - **Location:** `Services/PasswordGenerator.cs:47-54`
  - **Severity:** HIGH — directly reduces password entropy

  | Character Type | Pool Size | Per-char Probability | Expected in Uniform | Actual Bias |
  |---------------|-----------|---------------------|--------------------|----|
  | Uppercase letter | 26 | 1/4 × 1/26 = 0.96% | 1/76 = 1.32% | 0.73× (underrepresented) |
  | Lowercase letter | 26 | 1/4 × 1/26 = 0.96% | 1/76 = 1.32% | 0.73× (underrepresented) |
  | Digit | 10 | 1/4 × 1/10 = **2.50%** | 1/76 = 1.32% | **1.90×** (overrepresented) |
  | Special character | 14 | 1/4 × 1/14 = **1.79%** | 1/76 = 1.32% | **1.36×** (overrepresented) |

  Per-character Shannon entropy drops from log₂(76) = 6.25 bits to ~6.05 bits. For 12 fill characters, that is ~72.6 bits + 16.5 bits (4 forced seed chars) = **~89 bits actual** vs. **99.8 bits theoretical**. Fix by selecting uniformly from the full concatenated pool of all enabled character types.

- [ ] **Honour the `Require*` policy flags during generation.** If `RequireUppercase` is `false`, the generator still always adds an uppercase character. Each `Require*` flag should control whether that character type is included.
  - **Location:** `Services/PasswordGenerator.cs:31-34` (unconditionally adds one of each type)
  - **Tests ready to activate:** `GeneratePassword_WithRequireUppercaseFalse_ShouldExcludeUppercase`, `GeneratePassword_WithRequireLowercaseFalse_ShouldExcludeLowercase`, `GeneratePassword_WithRequireNumberFalse_ShouldExcludeDigits`, `GeneratePassword_WithRequireSpecialCharacterFalse_ShouldExcludeSpecials`, `GeneratePassword_WithOnlyLettersRequired_ShouldContainOnlyLetters`, `GeneratePassword_WithOnlyDigitsRequired_ShouldContainOnlyDigits`, `GeneratePassword_WithOnlySpecialsRequired_ShouldContainOnlySpecials`
  - **Active tests that will intentionally break (update them):** `GeneratePassword_IgnoresRequireUppercaseFalse_StillContainsUppercase`, `GeneratePassword_IgnoresRequireLowercaseFalse_StillContainsLowercase`, `GeneratePassword_IgnoresRequireNumberFalse_StillContainsDigit`, `GeneratePassword_IgnoresRequireSpecialCharacterFalse_StillContainsSpecial`

- [ ] **Enforce `MaximumLength`.** `GeneratePassword()` fills to `MinimumLength` but never caps at `MaximumLength`. Passwords should have a random length in `[MinimumLength, MaximumLength]`.
  - **Location:** `Services/PasswordGenerator.cs:36-41` (while loop fills to MinimumLength only)
  - **Test ready to activate:** `GeneratePassword_ShouldRespectMaximumLength`
  - **Active test that will intentionally break:** `GeneratePassword_CurrentlyIgnoresMaximumLength_UsesMinimumLength`

- [ ] **Handle character-pool exhaustion.** With rules 4+5 (unique digits/specials), a 64-char password can have at most 10 digits and 14 specials. The generator needs to gracefully fall back to letters when pools are exhausted, and reject configurations where the requested length is impossible to satisfy given the active constraints.
  - **Location:** `Services/PasswordGenerator.cs:36-41` (fill loop has no pool awareness)

- [ ] **Enforce an upper bound on password length.** `MinimumLength` can currently be `int.MaxValue`, causing unbounded `StringBuilder` allocation and potential OOM / denial-of-service.
  - **Location:** `Services/PasswordGenerator.cs:36` (while loop bound), `Models/PasswordPolicy.cs:7` (no max validation)
  - **Severity:** MEDIUM
  - **Fix:** Add a sensible cap (e.g., 4096) validated in `PasswordPolicy` construction or `GeneratePassword()` entry.

---

## P1 — Robustness (fail fast, prevent misuse)

- [ ] **Validate `PasswordPolicy` at construction time.** There is no input validation — `MinimumLength` could be 100 while `MaximumLength` is 16, or values could be negative. Move the PCI-DSS minimum check out of `GeneratePassword()` and add a `Validate()` method (or constructor validation) that catches invalid configurations early: `MaximumLength < MinimumLength`, negative values, minimum length too short to satisfy all required character types, etc.
  - **Location:** `Models/PasswordPolicy.cs` (all properties have public setters, no validation), `Services/PasswordGenerator.cs:27-28` (PCI check)
  - **Tests ready to activate:** `Policy_ShouldThrow_WhenMinimumLengthExceedsMaximumLength`, `Policy_ShouldThrow_WhenMinimumLengthIsNegative`, `Policy_ShouldThrow_WhenMaximumLengthIsNegative`, `Policy_ShouldThrow_WhenMinimumLengthBelowPciMinimum`, `Policy_ShouldThrow_WhenMinimumLengthTooShortForRequiredTypes`, `GeneratePassword_ShouldThrow_WhenMinimumLengthExceedsMaximumLength`
  - **Active tests that will intentionally break:** `Policy_AllowsZeroMinimumLength`, `Policy_AllowsNegativeMinimumLength`, `Policy_AllowsMinimumLengthGreaterThanMaximumLength`, `GeneratePassword_MinimumLengthExceedsMaximumLength_CurrentlyUsesMinimumLength`

- [ ] **Make `PasswordPolicy` immutable.** Public setters mean the policy can be mutated after the generator is constructed, which could cause unexpected behaviour mid-generation. Switch to `init`-only setters or a constructor with required parameters.
  - **Location:** `Models/PasswordPolicy.cs` (all 8 properties use `{ get; set; }`)
  - **Recommended approach:** Convert to a `record class` with positional parameters — gives immutability, value equality, `with` expressions, and `ToString()` for free. This is the idiomatic .NET 9 approach.
  - **Test ready to activate:** `Policy_ShouldNotAllowMutationAfterConstruction`

- [ ] **Implement `AvoidCommonPatterns`.** The flag is declared but never checked. Decide what patterns to reject (dictionary words, keyboard walks like "qwerty", date-like sequences) and implement the check.
  - **Location:** `Models/PasswordPolicy.cs:15` (property declaration), `Services/PasswordGenerator.cs` (never referenced)
  - **Active test documenting current no-op:** `GeneratePassword_CurrentlyIgnoresAvoidCommonPatterns`

- [ ] **Add null guard on `PasswordGenerator` constructor.** `new PasswordGenerator(null!)` throws a `NullReferenceException` deep in `GeneratePassword()` at line 27 with no useful message.
  - **Location:** `Services/PasswordGenerator.cs:14` (constructor)
  - **Severity:** LOW
  - **Fix:** `_policy = policy ?? throw new ArgumentNullException(nameof(policy));`

- [ ] **Fix exception type in `GeneratePassword()`.** Line 28 throws `ArgumentException`, but the invalid value is the `_policy` field (set at construction time), not a method argument. `ArgumentException` semantically means "a method argument was invalid."
  - **Location:** `Services/PasswordGenerator.cs:28`
  - **Severity:** LOW
  - **Fix:** Use `InvalidOperationException` ("object is in an invalid state") or move validation to the constructor so `ArgumentException` would be correct there.

- [ ] **Handle exceptions gracefully in `GenerateCommand.Execute()`.** Spectre.Console.Cli does not automatically catch exceptions thrown from `Execute`. An invalid policy configuration dumps a raw stack trace to the terminal.
  - **Location:** `Commands/GenerateCommand.cs:14` (Execute method, no try/catch)
  - **Severity:** MEDIUM
  - **Fix:** Catch expected exceptions, write a user-friendly error via `AnsiConsole.MarkupLine("[red]Error: {message}[/]")`, and return a non-zero exit code.

---

## P2 — Architecture & Refactoring (improve maintainability)

- [ ] **Break up `GeneratePassword()` into focused private methods.** It currently validates the policy, guarantees required character types, fills remaining length, and shuffles — all in one method. Extract each responsibility (e.g., `EnsureRequiredCharacters()`, `FillToLength()`, `ShufflePassword()`) for clarity and testability.
  - **Location:** `Services/PasswordGenerator.cs:16-54` (entire method)

- [ ] **Extract the shuffle into a reusable utility method.** The inline `OrderBy` shuffle in `GeneratePassword()` should be a standalone `Shuffle<T>()` method using Fisher-Yates, usable elsewhere and independently testable.
  - **Location:** `Services/PasswordGenerator.cs:42`

- [ ] **Make `RandomCharacterGenerator` an instance with an injectable character pool.** It is currently a static class with a hardcoded special character set. Converting it to an instance class that accepts allowed characters via its constructor would support customisable character pools and make it testable with deterministic fakes.
  - **Location:** `Utilities/RandomCharacterGenerator.cs` (entire file — `public static class`, all methods static)
  - **Consequence:** Currently impossible to write deterministic unit tests for the generation algorithm. All tests in `PasswordGeneratorTests.cs` are necessarily statistical/probabilistic because they cannot control randomness.

- [ ] **Improve character-type weighting in `GetRandomCharacter()`.** It currently gives equal 25% probability to all four character types regardless of how many characters of each type are already in the password or which types are enabled. Consider weighting toward underrepresented types to produce more balanced passwords, and only selecting from enabled types.
  - **Location:** `Services/PasswordGenerator.cs:47-54` (switch on `GetInt32(0, 4)`)
  - **See also:** P0 item on non-uniform distribution — this is the root cause.

- [ ] **Dependency injection / interface extraction.** Extract `IPasswordGenerator` and `IRandomCharacterGenerator` interfaces so the generator is testable with deterministic fakes and extensible for different generation strategies. Wire up via Spectre.Console.Cli's `TypeRegistrar`.
  - **Location:** `Services/PasswordGenerator.cs` (concrete class, no interface), `Commands/GenerateCommand.cs:16` (hardcodes `new PasswordPolicy()` and `new PasswordGenerator(policy)` — no injection seam)
  - **Note:** `GenerateCommand` also ignores the `CancellationToken` parameter it receives from Spectre.Console (line 14). Pass it through if generation becomes long-running (e.g., batch with breach checking).

- [ ] **Separate library from CLI.** Split into `SecurePasswordGenerator.Core` (class library) and `SecurePasswordGenerator.Cli` (console app) so the generator can be consumed as a NuGet package independently.

- [ ] **Separate bootstrap/wiring from `Program.cs`.** All configuration is currently inline. As the app grows with CLI arguments and DI, extract a configuration or bootstrap step to keep the entry point clean.
  - **Location:** `Program.cs` (entire file — 12 lines mixing Spectre.Console.Cli setup with version config)

- [ ] **Expose `SpecialCharacters` as a public constant.** `RandomCharacterGenerator.SpecialCharacters` is `private` but is duplicated as a string literal in both test files. Make it `public` (or `internal` with `InternalsVisibleTo`) and reference it from tests to prevent silent divergence.
  - **Duplication locations:**

  | File | Line | Value |
  |------|------|-------|
  | `Utilities/RandomCharacterGenerator.cs` | 9 | `"!@#$%^&*()-_=+"` (authoritative source) |
  | `SecurePasswordGenerator.Tests/PasswordGeneratorTests.cs` | 13 | `"!@#$%^&*()-_=+"` (copy) |
  | `SecurePasswordGenerator.Tests/RandomCharacterGeneratorTests.cs` | 7 | `"!@#$%^&*()-_=+"` (copy) |

- [ ] **Read version from assembly metadata instead of hardcoding.** The version string is duplicated and will inevitably drift.
  - **Duplication locations:**

  | File | Line | Value |
  |------|------|-------|
  | `Program.cs` | 9 | `SetApplicationVersion("0.2.1")` |
  | `SecurePasswordGenerator.csproj` | 8 | `<Version>0.2.1</Version>` |

  - **Fix:** Read at runtime via `Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion` so the `.csproj` `<Version>` is the single source of truth.

---

## P3 — Code Cleanup (quick wins)

- [ ] **Remove unused `using` statements.** Implicit usings are enabled (`<ImplicitUsings>enable</ImplicitUsings>`) which covers `System`, `System.Linq`, `System.Collections.Generic`, etc.

  | File | Line(s) | Redundant Using |
  |------|---------|----------------|
  | `Program.cs` | 1 | `using System;` |
  | `Utilities/RandomCharacterGenerator.cs` | 1 | `using System;` |
  | `Services/PasswordGenerator.cs` | 1 | `using System;` |
  | `Services/PasswordGenerator.cs` | 2 | `using System.Linq;` |

  Note: `using System.Text;` (line 4) and `using System.Security.Cryptography;` (line 3) in `PasswordGenerator.cs` are NOT implicit — keep those.

- [ ] **Replace magic numbers with character literals in `RandomCharacterGenerator.cs`.** ASCII ranges should use character arithmetic for readability.

  | Current (line) | Replacement |
  |----------------|-------------|
  | `GetInt32(65, 91)` (line 12) | `GetInt32('A', 'Z' + 1)` |
  | `GetInt32(97, 123)` (line 15) | `GetInt32('a', 'z' + 1)` |
  | `GetInt32(48, 58)` (line 18) | `GetInt32('0', '9' + 1)` |

- [ ] **Adopt consistent namespace style.** Standardise on file-scoped namespaces throughout.

  | File | Current Style |
  |------|--------------|
  | `Program.cs` | No namespace |
  | `Commands/GenerateCommand.cs` | File-scoped |
  | `Models/PasswordPolicy.cs` | Block-scoped |
  | `Services/PasswordGenerator.cs` | Block-scoped |
  | `Utilities/RandomCharacterGenerator.cs` | Block-scoped |
  | All test files | File-scoped |

- [ ] **Replace magic number `4` in `GetRandomCharacter()`.** `RandomNumberGenerator.GetInt32(0, 4)` should use a named constant or derive the value from the number of enabled character types.
  - **Location:** `Services/PasswordGenerator.cs:47`

- [ ] **Seal classes not designed for inheritance.** None of these classes are designed for extension — sealing enables JIT devirtualisation and communicates intent.

  | Class | File |
  |-------|------|
  | `PasswordPolicy` | `Models/PasswordPolicy.cs` |
  | `PasswordGenerator` | `Services/PasswordGenerator.cs` |
  | `GenerateCommand` | `Commands/GenerateCommand.cs` |
  | `RandomCharacterGenerator` | `Utilities/RandomCharacterGenerator.cs` |

- [ ] **Add `[Description]` attribute to `GenerateCommand`.** The Spectre.Console.Cli `--help` output currently shows no description of what the tool does.
  - **Location:** `Commands/GenerateCommand.cs:8` (class declaration, no attribute)
  - **Fix:** `[Description("Generate a cryptographically secure random password")]`

---

## P4 — CLI & Usability (user-facing features)

Based on competitive analysis of pwgen, Bitwarden CLI, keepassxc-cli, gopass, 1Password CLI, xkpasswd, and diceware.

- [ ] **Command-line arguments.** Accept options like `--length`, `--count`, `--no-special`, `--exclude-chars`, `--policy-file` via `GenerateCommand.Settings` (Spectre.Console.Cli infrastructure is already in place — the Settings class is just empty).
  - **Location:** `Commands/GenerateCommand.cs:10-12` (empty `Settings` class with no `[CommandOption]` attributes)

- [ ] **Generate multiple passwords at once.** Add a `--count N` option and a `GeneratePasswords(int count)` method. When stdout is a TTY, display in a grid format (like pwgen) so the user can visually pick one.
  - **Implemented by:** pwgen, Bitwarden CLI, keepassxc-cli, gopass, xkpasswd

- [ ] **Copy to clipboard with auto-clear.** Optionally copy the generated password to the system clipboard and clear it after a configurable timeout (default 45 seconds, matching gopass and keepassxc-cli). Consider the cross-platform `TextCopy` NuGet package.
  - **Implemented by:** gopass (45s), keepassxc-cli (45s), 1Password CLI
  - **Note:** keepassxc-cli blocks the terminal during the timeout; a background timer approach is better.

- [ ] **Exit codes & stderr.** Return non-zero exit codes on error and write diagnostics to stderr so the tool composes well in scripts (`password=$(spg)` should not capture error messages).
  - **Location:** `Commands/GenerateCommand.cs:14` (always returns 0)

- [ ] **Quiet / pipe-friendly mode.** Add `--quiet` (or `-q`) that outputs only the password with no label or decoration. Auto-detect when stdout is not a TTY (piped) and default to quiet mode, so `spg | xclip` works without extra flags.
  - **Location:** `Commands/GenerateCommand.cs:20-21` (always prints "Generated Secure Password:" label + ANSI markup)
  - **Implemented by:** pwgen (auto-detects TTY), diceware (`--quiet`)
  - **Why it matters:** The label line `"Generated Secure Password:"` is unwanted noise in pipelines. Currently `$(dotnet run)` captures both lines.

- [ ] **Exclude ambiguous characters.** Add `--exclude-ambiguous` (or `-B`, following `pwgen` convention) to remove visually confusable characters.
  - **Characters to exclude:** `0O` (zero/letter O), `1lI` (one/lowercase L/uppercase I), `` `' `` (backtick/apostrophe)
  - **Implemented by:** pwgen (`-B`), Bitwarden CLI, keepassxc-cli
  - **Why it matters:** Reduces support tickets when passwords are communicated verbally, printed, or displayed in ambiguous fonts.

- [ ] **Exclude specific characters.** Add `--exclude-chars "{}[]|"` to remove characters that a target system rejects.
  - **Implemented by:** keepassxc-cli, NSA RandPassGenerator (`-pwcustom`)
  - **Why it matters:** Critical for systems with restricted special-character sets (e.g., some LDAP systems reject certain specials).

- [ ] **Minimum count per character class.** Add `--min-number N` and `--min-special N` options to require at least N digits or N symbols (beyond just "at least one").
  - **Implemented by:** Bitwarden CLI (`--min-number`, `--min-special`)

- [ ] **Color-coded character types in output.** Highlight uppercase, lowercase, digits, and special characters in different colours for visual verification.
  - **Implementation:** Use Spectre.Console's `Markup` class: `[blue]3[/]` for digits, `[red]![/]` for specials, etc. Trivial to implement.

- [ ] **JSON output mode.** Add `--format json` that outputs structured data for machine consumption and `jq` piping.
  - **Example output:**
    ```json
    {"password": "xK3!mNp9@wR2", "length": 12, "entropy": 85.2,
     "classes": {"upper": 3, "lower": 4, "digit": 3, "special": 2}}
    ```
  - **Implemented by:** motus, AWS Secrets Manager CLI

- [ ] **Verbose mode.** Add `--verbose` (or `-v`) that displays the applied policy, character pool size, entropy bits, and character class breakdown alongside the password. Useful for debugging and trust-building.
  - **Example output:**
    ```
    Generated Secure Password: xK3!mNp9@wR2
    Length: 12 | Pool: 76 chars | Entropy: 74.9 bits
    Classes: 3 upper, 4 lower, 3 digit, 2 special
    Policy: PCI-DSS (min 12, all classes required)
    ```

- [ ] **Presets / profiles.** Add `--preset <name>` with built-in profiles. Saves users from remembering exact flag combinations.

  | Preset | Length | Classes | Notes |
  |--------|--------|---------|-------|
  | `web` | 16 | All | General-purpose default |
  | `wifi` | 63 | Alphanumeric | WPA2 max passphrase length |
  | `pin` | 6 | Digits only | Numeric PIN |
  | `passphrase` | 4 words | Words + separator | Diceware mode |
  | `pci` | 12 | All | PCI-DSS minimum compliance |
  | `nist` | 16 | All | NIST SP 800-63B aligned |

  - **Implemented by:** xkpasswd (complexity levels 1–6)

- [ ] **Interactive mode.** If invoked with `--interactive` (or optionally by default when no args + TTY), prompt the user for options using Spectre.Console's `SelectionPrompt` and `TextPrompt` widgets.
  - **Implementation:** Spectre.Console already provides `AnsiConsole.Prompt`, `SelectionPrompt<T>`, and `TextPrompt<T>`.

- [ ] **Screen masking / reveal toggle.** Add `--mask` to avoid printing the password in cleartext. Display masked output (e.g., `************`) with a "press any key to reveal / hide" prompt. Reduces shoulder-surfing and terminal scrollback exposure.
  - **Location:** `Commands/GenerateCommand.cs:21` (currently uses `AnsiConsole.WriteLine(password)` — plaintext, persists in scrollback)

- [ ] **No-colour flag.** Add `--no-color` to disable ANSI escape codes for log files, CI environments, or accessibility. Also respect the `NO_COLOR` environment variable ([no-color.org](https://no-color.org) convention).

---

## P5 — Password Strength & Security (advanced features)

- [ ] **Entropy bits display.** Calculate and display the password's entropy in bits (`Entropy: 85.2 bits`) based on the actual character pool size and generation constraints. Formula: `log₂(pool_size) × length` for uniform random; adjusted for non-uniform or constrained generation.
  - Show by default in normal mode, suppress in `--quiet` mode.
  - **Implemented by:** sebastienrousseau/password-generator, zxcvbn-cli
  - This is the single most valuable feedback feature — it lets users make informed decisions about password strength.

- [ ] **Crack time estimation.** Display a human-friendly crack time alongside entropy. More intuitive than raw bits for non-technical users.

  | Attack Scenario | Speed | 85-bit password |
  |----------------|-------|-----------------|
  | Online (rate-limited) | 1,000/sec | ~1.2 × 10¹⁹ years |
  | Online (fast) | 100,000/sec | ~1.2 × 10¹⁷ years |
  | Offline (MD5) | 10 billion/sec | ~1.2 × 10¹² years |
  | Offline (bcrypt) | 100,000/sec | ~1.2 × 10¹⁷ years |

  - **Implemented by:** zxcvbn, zxcvbn-cli (Dropbox)

- [ ] **Password strength estimator.** Integrate an algorithm like zxcvbn to give users heuristic feedback on the generated password's resistance to pattern-based attacks (keyboard walks, dictionary words, date sequences, repeats).
  - **Implemented by:** zxcvbn (0–4 strength score), zxcvbn-cli

- [ ] **Configurable character pools.** Allow users to supply a custom set of allowed special characters via `--special-chars "!@#$"` or a config file. Some systems restrict which specials are valid.
  - **Implemented by:** NSA RandPassGenerator (`-pwcustom`), keepassxc-cli

- [ ] **Passphrase / diceware mode.** Add an alternative mode (`--passphrase` or a `passphrase` subcommand) that generates passphrases from a word list, with configurable options.
  - **Implemented by:** Bitwarden CLI, keepassxc-cli, gopass, diceware, xkpasswd
  - **Implementation:** Embed the [EFF long word list](https://www.eff.org/dice) (7,776 words) as an assembly resource. Each word provides log₂(7776) = 12.9 bits of entropy. Four words = 51.7 bits; six words = 77.5 bits.
  - **Options:** `--words N` (default 4), `--separator <char>` (default `-`), `--capitalize` (capitalize first letter of each word), `--add-number` (append a random digit)
  - **Example:** `correct-horse-battery-staple`

- [ ] **PIN / numeric-only mode.** Add `--pin N` to generate a numeric PIN of N digits. Useful for systems that only accept digits (ATMs, phone unlock, verification codes).
  - **Implemented by:** Bitwarden CLI
  - **Entropy:** A 6-digit PIN has log₂(10⁶) = 19.9 bits. Display this to set expectations.

- [ ] **No-vowels mode.** Add `--no-vowels` (or `-v` if not taken) to avoid accidentally generating offensive substrings in auto-provisioned passwords that end users will see.
  - **Implemented by:** pwgen (`-v`)

- [ ] **Breach / dictionary check.** Optionally check the generated password's SHA-1 prefix against the Have I Been Pwned Passwords API (k-anonymity model) to confirm it hasn't appeared in known breaches. Regenerate automatically if a match is found.
  - **API:** `GET https://api.pwnedpasswords.com/range/{first5-of-SHA1}` — only the first 5 hex chars of the SHA-1 hash leave the machine. The full password is never transmitted.
  - **Implementation:** Single `HttpClient.GetStringAsync()` call, parse response, check if the remaining hash suffix appears. If so, regenerate and retry (with a limit).

- [ ] **Reduce password copies in memory.** The current implementation creates 3–4 copies of the password in managed memory that cannot be zeroed:
  - **Location and copies:**

  | Copy # | Location | How Created |
  |--------|----------|-------------|
  | 1 | `StringBuilder` internal buffer | `Services/PasswordGenerator.cs:30` |
  | 2 | `ToString()` result | `Services/PasswordGenerator.cs:42` (`.ToString()`) |
  | 3 | `ToCharArray()` array | `Services/PasswordGenerator.cs:42` (`.ToCharArray()`) |
  | 4 | Final `new string(...)` | `Services/PasswordGenerator.cs:42` (return value) |

  - **Fix:** Use a `char[]` from the start, shuffle in-place via Fisher-Yates, convert to `string` once for output, then `Array.Clear()` the buffer. Won't eliminate the final `string` (immutable, GC-managed), but reduces exposure from 4 copies to 2.
  - **Note:** `SecureString` is deprecated in .NET Core. The managed `string` limitation should be documented as a known constraint. For short-lived CLI processes, the exposure window is minimal.

---

## P6 — Distribution & Integration (release pipeline)

- [ ] **GitHub Actions CI.** Add a workflow that builds, runs tests, and publishes releases on tag push.

- [ ] **Publish as a .NET global tool** (`dotnet tool install -g`) for easy installation. Requires `.csproj` additions:
  - **Location:** `SecurePasswordGenerator.csproj` (missing properties)
  ```xml
  <PackAsTool>true</PackAsTool>
  <ToolCommandName>spg</ToolCommandName>
  ```

- [ ] **Cross-platform single-file publish.** Add publish profiles for self-contained single-file executables on Windows, macOS, and Linux.

- [ ] **Add `global.json` to pin the SDK version.** Without it, different environments may build with different .NET SDK versions, producing inconsistent binaries.
  - **Location:** Project root (file does not exist)
  - **Example:**
  ```json
  { "sdk": { "version": "9.0.100", "rollForward": "latestPatch" } }
  ```

- [ ] **Enable NuGet lock files.** NuGet treats version numbers as minimums by default (e.g., `Version="0.53.1"` means `>=0.53.1`), so without a lock file a compromised newer version could be silently pulled in.
  - **Location:** `Directory.Build.props` (does not exist yet)
  - **Fix:** Add `<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>`, run `dotnet restore`, and commit the generated `packages.lock.json`.

  | Current dependency | Pinned version | Effective meaning without lock file |
  |-------------------|----------------|-------------------------------------|
  | `Spectre.Console.Cli` | `0.53.1` | `>= 0.53.1` (any newer auto-resolved) |
  | `xunit` | `2.9.3` | `>= 2.9.3` |
  | `xunit.runner.visualstudio` | `3.0.2` | `>= 3.0.2` |
  | `Microsoft.NET.Test.Sdk` | `17.12.0` | `>= 17.12.0` |
  | `coverlet.collector` | `6.0.4` | `>= 6.0.4` |

- [ ] **Add `Directory.Build.props` for shared build properties.** Both `.csproj` files duplicate common settings:

  | Property | `SecurePasswordGenerator.csproj` | `Tests.csproj` |
  |----------|----------------------------------|----------------|
  | `TargetFramework` | `net9.0` | `net9.0` |
  | `ImplicitUsings` | `enable` | `enable` |
  | `Nullable` | `enable` | `enable` |

  Centralise in a `Directory.Build.props` at the solution root.

- [ ] **Generate SBOM (Software Bill of Materials).** Configure `dotnet CycloneDX` or similar to produce an SBOM with each release. Increases supply-chain trust for a security tool.

- [ ] **Shell completions.** Generate or hand-write tab-completion scripts for bash, zsh, fish, and PowerShell. Significantly improves discoverability for power users. Spectre.Console.Cli does not auto-generate these.

- [ ] **User config file / environment variables.** Support `~/.config/spg/config.json` or environment variables for user defaults, so common flags don't need to be repeated every invocation.

  | Env Variable | Effect |
  |-------------|--------|
  | `SPG_DEFAULT_LENGTH` | Default password length |
  | `SPG_DEFAULT_PRESET` | Default preset name |
  | `SPG_NO_COLOR` | Disable colour (also respect `NO_COLOR`) |
  | `SPG_CLIPBOARD_TIMEOUT` | Clipboard clear timeout in seconds |

---

## P7 — Testing (expand coverage)

- [ ] **Add tests for `GenerateCommand` (CLI layer).** There are currently **zero tests** for the command layer — the entire CLI surface is untested.
  - **Location:** No test file exists for `GenerateCommand`
  - **Severity:** HIGH
  - **Implementation:** Use Spectre.Console.Cli's `CommandAppTester` to verify:
    - Exit code 0 on success
    - Correct output format (password printed, label present)
    - Non-zero exit code and user-friendly error message on invalid input
    - End-to-end integration from CLI args → policy → generator → output
    - `--version` flag behaviour
    - Future: `--quiet`, `--count`, `--length`, etc.

- [ ] **Add tests for extreme-length generation.** Current `[Theory]` tests go up to 128 characters. Add tests for:
  - The upper bound (e.g., 1024) — verify the generator completes in acceptable time
  - Over the bound (e.g., 10000) — verify rejection after the length cap is added (see P0)
  - **Location:** `SecurePasswordGenerator.Tests/PasswordGeneratorTests.cs` (Theory data stops at 128)

- [ ] **Add thread-safety test.** `PasswordGenerator.GeneratePassword()` uses a local `StringBuilder` so it is technically reentrant, but a concurrent-call test would document this guarantee and catch regressions if instance state is added later.
  - **Implementation:** Spin up N `Task.Run` calls sharing one `PasswordGenerator` instance, collect results, verify all passwords are valid and distinct.

- [ ] **Reference `SpecialCharacters` from source in tests.** Both test files hardcode the special character string — will silently diverge if the source changes. Once the constant is made `public` (see P2), update tests.

  | Test File | Line | Current |
  |-----------|------|---------|
  | `PasswordGeneratorTests.cs` | 13 | `private const string SpecialCharacters = "!@#$%^&*()-_=+";` |
  | `RandomCharacterGeneratorTests.cs` | 7 | `private const string SpecialCharacters = "!@#$%^&*()-_=+";` |

- [ ] **Configure code coverage reporting.** `coverlet.collector` (v6.0.4) is already a test project dependency but produces no output because no format or threshold is configured.
  - **Location:** `SecurePasswordGenerator.Tests/SecurePasswordGenerator.Tests.csproj:11`
  - **Fix:** Add to test `.csproj`:
    ```xml
    <CoverletOutputFormat>cobertura</CoverletOutputFormat>
    <ThresholdType>line</ThresholdType>
    <Threshold>80</Threshold>
    ```
  - Wire into CI (P6) to fail builds below the threshold.

- [ ] **Add `PasswordGenerator` constructor null-guard test.** Once the null guard is added (see P1), verify `new PasswordGenerator(null!)` throws `ArgumentNullException`.

- [ ] **Add probabilistic collision diagnostic.** `GeneratePassword_ProducesUniquePasswords` asserts all 50 passwords are unique but gives no diagnostic information about *which* passwords collided on failure. Use `Assert.Equal(50, passwords.Count)` with a custom message or `Assert.Distinct(passwords)` for better output.
  - **Location:** `SecurePasswordGenerator.Tests/PasswordGeneratorTests.cs:98`

---

## P8 — Project Hygiene & .NET Best Practices

- [ ] **Add `.editorconfig` for code style enforcement.** There is no `.editorconfig`. Given the inconsistent namespace styles and formatting variations, an `.editorconfig` would enforce consistency automatically across IDEs and `dotnet format`.
  - **Key rules to include:** `csharp_style_namespace_declarations = file_scoped`, indent style/size, `dotnet_sort_system_directives_first`, `csharp_prefer_braces`, etc.

- [ ] **Add XML doc comments on public APIs.** None of the 4 public classes or their public methods have `<summary>` documentation. Enable documentation generation in the `.csproj`:
  ```xml
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  ```
  Essential if the library is consumed as a NuGet package (see P2 — separate library from CLI).

  | Public Class | Public Members Without Docs |
  |-------------|---------------------------|
  | `PasswordPolicy` | 8 properties |
  | `PasswordGenerator` | Constructor, `GeneratePassword()` |
  | `RandomCharacterGenerator` | `GetRandomUppercase()`, `GetRandomLowercase()`, `GetRandomDigit()`, `GetRandomSpecialCharacter()`, `GetRandomCharacter()` |
  | `GenerateCommand` | `Execute()` |

- [ ] **Expand `.gitignore` for full .NET coverage.** The current `.gitignore` (27 lines) is functional but minimal compared to the standard GitHub template (350+ lines). Add at minimum:

  | Pattern | Purpose | Status |
  |---------|---------|--------|
  | `*.nupkg` / `*.snupkg` | NuGet package artifacts | Missing |
  | `publish/` | `dotnet publish` output | Missing |
  | `*.log` | Runtime/debug log files | Missing |
  | `*.cache` | MSBuild cache files | Missing |
  | `*.rsuser` | Visual Studio user files | Missing |
  | `*.DotSettings.user` | ReSharper user settings | Missing |
  | `project.lock.json` | NuGet lock (old format) | Missing |
  | `BundleArtifacts/` | Build artifacts | Missing |
  | `PublishProfiles/` | VS publish profiles | Missing |
  | `TestResults/` | `dotnet test` output | Missing |
  | `*.swp` / `*~` | Editor temp files | Missing |
  | `packages.lock.json` | (If not committing lock file) | Conditional |

  Consider adopting the full [GitHub .NET gitignore template](https://github.com/github/gitignore/blob/main/VisualStudio.gitignore).

- [ ] **Review git author metadata before publishing.** All 7 commits contain `Salman Ahmed <ssahmed532@gmail.com>` as the author. If the repo will be made public, decide whether exposing a real name and personal email is acceptable. If not, rewrite history with `git filter-repo` before publishing — this is a one-time, irreversible operation.

---

## P9 — Framework Upgrade (when .NET 10 is stable)

- [ ] **Upgrade from .NET 9.0 to .NET 10.** Update `TargetFramework` in both `.csproj` files (or `Directory.Build.props` if centralised by then) from `net9.0` to `net10.0`, verify all dependencies are compatible, and take advantage of any new language features or performance improvements.

- [ ] **Async-friendly API.** Not critical today, but if breach-checking (HTTP call) or file-based word lists (file I/O) are added, having `async` variants of the generation pipeline avoids blocking.

---
codex: 1
project: MindAttic.Helpers
code: HLP
layer: bible
status: living
updated: 2026-06-07
---

# MindAttic.Helpers — Project Bible
> Single source of truth for what MindAttic.Helpers IS, is NOT, and the rules that keep it coherent.
> README says how to build/run; this says how to think about the system.

## 1. The one sentence {#HLP-§1}
MindAttic.Helpers is a small, zero-dependency .NET library of pure, deterministic
utility helpers shared across the MindAttic ecosystem — each helper is a single
static class with a stable, faithfully-ported behaviour.

## 2. The product promise {#HLP-§2}
- **Deterministic by construction.** Given the same input, a helper always returns
  the same output, anywhere it runs (server, client, build step). See
  [`AbstractArtGenerator`](#HLP-§4) and [`PiHelper`](#HLP-§4).
- **Zero runtime dependencies.** The library references only the .NET base class
  library (`System.*`). No NuGet runtime dependencies ship with the package.
- **Drop-in, self-contained.** Each helper does one thing and ships no fonts, files,
  or network calls. `AbstractArtGenerator.DataUri(seed)` is usable directly in an
  `<img src>`; `PiHelper.Calculate(n)` returns a value record, no I/O.
- **Faithful ports.** Where a helper mirrors an existing implementation (e.g. the
  generative art on mindattic.com), it reproduces that behaviour bit-for-bit so the
  output matches across the stack.

## 3. What it is NOT {#HLP-§3}
- **NOT an application or service.** There is no host, no DI graph, no entry point —
  it is a class library (`MindAttic.Helpers.csproj`, `IsPackable=true`).
- **NOT a grab-bag of stateful utilities.** Helpers are pure static classes with no
  shared mutable state, no singletons, no configuration.
- **NOT a dependency hub.** It must not take on third-party runtime dependencies; a
  helper that needs one belongs in a different package.
- **NOT a UI / rendering toolkit.** `AbstractArtGenerator` emits SVG markup; it does
  not rasterize, lay out, or render anything itself.
- **NOT a precision allocator / memory manager.** `PiHelper`'s memory guard is a
  best-effort safety valve over a recent GC snapshot, not a live or exact gauge.

## 4. Architecture canon {#HLP-§4}

```
                 MindAttic.Helpers (net10.0, IsPackable)
                 ┌───────────────────────────────────────────┐
   seed:string ─►│  AbstractArtGenerator (static)            │─► SVG / data URI
                 │    Rng (FNV-1a → LCG, struct)             │
                 │                                            │
   places:int  ─►│  PiHelper (static)                        │─► PiResult (record struct)
                 │    Gibbons unbounded spigot + mem guard   │
                 └───────────────────────────────────────────┘
                              ▲ referenced by ▲
                 MindAttic.Helpers.Tests (NUnit, IsPackable=false)
```

### 4.1 Projects
- `MindAttic.Helpers/MindAttic.Helpers.csproj` — the library. `net10.0`,
  `RootNamespace`/`AssemblyName` = `MindAttic.Helpers`, `IsPackable=true`,
  `GenerateDocumentationFile=true`, MIT, packs `README.md`.
- `MindAttic.Helpers.Tests/MindAttic.Helpers.Tests.csproj` — NUnit test project
  (`IsPackable=false`), references the library.
- `MindAttic.Helpers.slnx` — solution stitching both projects.

### 4.2 Domain model (NOUNS)
- **Helper** — a `public static` class in namespace `MindAttic.Helpers`; pure,
  deterministic, dependency-free.
- **`AbstractArtGenerator`** — turns a string `seed` into a stable 300×300 SVG
  fingerprint. Holds the 16 curated `Palettes` and a private `Rng` struct.
- **`Rng`** — private struct: FNV-1a (32-bit) seed over UTF-16 code units, advanced
  by a Numerical-Recipes LCG; reproduces the mindattic.com JS stream bit-for-bit.
- **`PiHelper`** — computes decimal digits of π via Gibbons' unbounded spigot, with a
  memory safety valve.
- **`PiHelper.PiResult`** — `readonly record struct` (Value, DecimalPlacesProduced,
  StoppedForMemory, FreeMemoryFraction): the outcome of a `Calculate` run.

### 4.3 Key services (VERBS)
- `AbstractArtGenerator.Svg(seed, initial?)` — raw 300×300 SVG markup.
- `AbstractArtGenerator.DataUri(seed, initial?)` — base64 `data:image/svg+xml` URI.
- `PiHelper.Calculate(decimalPlaces, minFreeMemoryFraction = 0.33)` — produce π to
  `decimalPlaces` places, stopping early if free RAM drops below the fraction.

## 5. The Laws {#HLP-§5}
This project **inherits the org-wide House Rules** verbatim — see
[`MindAttic.HouseRules.md`](../../MindAttic.HouseRules.md). Do not restate them here.
The most load-bearing for this library:
- [HOUSE-LAW-1 — Whole-number versioning](../../MindAttic.HouseRules.md#HOUSE-LAW-1)
  (`<Version>1.0.0</Version>` today; next release is `2.0.0`).
- [HOUSE-LAW-8 — Definition of done is verified, not asserted](../../MindAttic.HouseRules.md#HOUSE-LAW-8).
- [HOUSE-LAW-9 — `psst` only on explicit request](../../MindAttic.HouseRules.md#HOUSE-LAW-9).

Project-specific laws:

### HLP-LAW-1 — Zero runtime dependencies {#HLP-LAW-1}
The library references only the .NET base class library. No third-party runtime
`PackageReference` may be added to `MindAttic.Helpers.csproj`. A helper that needs a
dependency does not belong here. (Test-only dependencies in the Tests project are fine.)

### HLP-LAW-2 — Helpers are pure, deterministic, static {#HLP-LAW-2}
Every helper is a `public static` class with no shared mutable state and no I/O
(no files, no network). The same input yields the same output everywhere. Any
unavoidable environmental reading (e.g. `PiHelper`'s RAM check) must be a clearly
documented, opt-out best-effort guard — never silent nondeterminism in the result.

### HLP-LAW-3 — Faithful ports stay bit-for-bit {#HLP-LAW-3}
When a helper ports an existing implementation (e.g. `AbstractArtGenerator` mirrors
mindattic.com's `generateProjectArt`), it must consume its RNG in the same order and
reuse data tables verbatim, so output is identical across the stack. Determinism is
locked by tests; changing the stream is a breaking change requiring an amendment.

### HLP-LAW-4 — Every helper is locked by tests {#HLP-LAW-4}
No helper ships without NUnit tests asserting its core invariant (determinism /
correctness), output shape, and edge/error cases. A behaviour is `✅` in
[USER_STORIES](USER_STORIES.md) only when a named test proves it.

## 6. Verified state {#HLP-§6}
Evidence re-confirmed **2026-06-07** on `net10.0` (Windows, .NET 10.0.8):
- ✅ **Build**: `dotnet build` → Build succeeded. 0 Warning(s), 0 Error(s).
- ✅ **Tests**: `dotnet test` → **16 passed, 0 failed, 0 skipped** (~1.4 s total).
  - `AbstractArtGeneratorTests` — 7 tests (determinism, distinctness, well-formed
    SVG, data-URI round-trip, initial override + default, 16-palette shape).
  - `PiHelperTests` — 9 tests (bare 3, 3.1415, 99-place reference match, determinism,
    prefix-extension, guard disabled, impossible threshold stops at the interval,
    negative-places + out-of-range-threshold throw).
- Proven helpers: [`AbstractArtGenerator`](#HLP-§4), [`PiHelper`](#HLP-§4).
- See [USER_STORIES.md](USER_STORIES.md) for per-capability test citations.

## 7. Active frontier {#HLP-§7}
- No open RFCs beyond the seed example — see [docs/rfc/](rfc/).
- Backlog and partial/planned work live in
  [USER_STORIES.md → Priority backlog](USER_STORIES.md).
- Direction: grow the helper set (each new helper a pure static class with tests),
  keeping HLP-LAW-1 (zero deps) intact. Candidate additions are tracked as ⬜ stories.

## 8. Quality bar {#HLP-§8}
A helper is "done" (`✅`) when:
1. It is a pure, dependency-free static class (HLP-LAW-1, HLP-LAW-2).
2. It builds clean with `GenerateDocumentationFile=true` (XML doc comments on all
   public members — no missing-doc warnings).
3. NUnit tests lock its core invariant, output shape, and error/edge cases
   (HLP-LAW-4); `dotnet test` is green.
4. Public API is documented in this BIBLE §4.3 and cited by a story in USER_STORIES.
5. Versioning follows [HOUSE-LAW-1](../../MindAttic.HouseRules.md#HOUSE-LAW-1).

## 9. Glossary {#HLP-§9}
- **Helper** — a pure static utility class in `MindAttic.Helpers`.
- **Seed** — the input string to `AbstractArtGenerator`; hashed to drive the RNG.
- **FNV-1a** — Fowler–Noll–Vo 1a, the 32-bit string hash seeding the RNG.
- **LCG** — Linear Congruential Generator (Numerical Recipes constants) advancing the
  RNG stream.
- **Palette** — one of 16 `[gradientStart, gradientEnd, accent]` colour triples.
- **Data URI** — a `data:image/svg+xml;base64,…` string embeddable in `<img src>`.
- **Spigot algorithm** — Gibbons' unbounded algorithm streaming one correct π digit
  per step without knowing the final length up front.
- **`PiResult`** — the `readonly record struct` returned by `PiHelper.Calculate`.
- **Memory guard** — `PiHelper`'s best-effort RAM check (default stop below 33% free).

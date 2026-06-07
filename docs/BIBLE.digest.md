AUTHORITATIVE - full detail in docs/BIBLE.md

# MindAttic.Helpers - Bible Digest (generated)
<!-- generatedFrom: HLP-BIBLE. Do not hand-edit; run tools/codex.ps1 digest. -->

## 1. The one sentence
MindAttic.Helpers is a small, zero-dependency .NET library of pure, deterministic
utility helpers shared across the MindAttic ecosystem — each helper is a single
static class with a stable, faithfully-ported behaviour.

## 3. What it is NOT
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

## 5. The Laws
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

## 9. Glossary
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

## Status index (stories)
- done: 17  partial: 2  planned: 0  cut: 0

## Latest amendment
- HLP-A1 — Adopt the Codex documentation standard (supersedes —)

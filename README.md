# MindAttic.Helpers

Small, dependency-free .NET (`net10.0`) library of pure, deterministic helpers shared
across the MindAttic ecosystem. Every helper is a `public static` class: no shared
mutable state, no I/O beyond a documented, opt-out best-effort guard, no third-party
runtime dependencies — BCL only. See [docs/BIBLE.md](docs/BIBLE.md) for the full
architecture canon and the project's Laws; this file is the practical how-to.

Two helpers today:

- **`AbstractArtGenerator`** — a deterministic generative-art engine that turns any
  string seed into a stable SVG "fingerprint" image. The same seed always produces the
  same picture, making it ideal for avatars, project tiles, or persona portraits keyed
  by a slug. It's a faithful port of the generative art on **mindattic.com**: a deep
  gradient ground, a scatter of translucent shapes, and one bold accent letter.
- **`PiHelper`** — streams decimal digits of π using Jeremy Gibbons' unbounded spigot
  algorithm (arbitrary-precision `BigInteger` state). Includes a memory guard that
  stops cleanly when free RAM drops below a configurable fraction, so very large digit
  counts never OOM.

## Public API catalog

### `AbstractArtGenerator` (static class)

| Member | Signature | Description |
|---|---|---|
| `Palettes` | `static readonly IReadOnlyList<string[]> Palettes` | The 16 curated `[gradientStart, gradientEnd, accent]` colour triples (deep teal/indigo/purple/charcoal grounds, each with one neon accent) — reused verbatim from mindattic.com. |
| `Svg` | `static string Svg(string seed, char? initial = null)` | The raw 300×300 SVG markup for `seed`. The overlaid letter defaults to the first alphanumeric character of `seed` (or `?` if none); pass `initial` to override it. |
| `DataUri` | `static string DataUri(string seed, char? initial = null)` | `Svg(seed, initial)` wrapped as a base64 `data:image/svg+xml` URI — drop straight into `<img src>` or a CSS `background-image: url(...)`. |

**How it works:** the seed string is hashed with FNV-1a (32-bit) and advanced by a
Numerical-Recipes LCG (private `Rng` struct) — bit-for-bit the same deterministic
stream as the mindattic.com JS. That stream picks one of the 16 palettes, a
random-direction two-stop linear gradient, 5–8 translucent shapes (circles, rotated
rectangles, triangles) that are allowed to bleed past the 300×300 frame, and finally
overlays a single large initial letter in the accent colour. Output is a self-contained
SVG string: no network, no files, no fonts to ship. Feed it the same slug anywhere —
server, client, build step — and you get pixel-identical art.

```csharp
using MindAttic.Helpers;

// A base64 data URI — drop straight into <img src> or CSS background-image:
string uri = AbstractArtGenerator.DataUri("persona-0042");

// Or the raw 300×300 SVG, with an optional letter override:
string svg = AbstractArtGenerator.Svg("persona-0042", initial: 'M');
```

```razor
<img src="@AbstractArtGenerator.DataUri(persona.Id)" alt="@persona.Name" />
```

```csharp
// Determinism is the whole point: the same seed always yields the same output.
Assert.That(AbstractArtGenerator.Svg("persona-0042"),
            Is.EqualTo(AbstractArtGenerator.Svg("persona-0042")));

// Different seeds give visibly different art.
Assert.That(AbstractArtGenerator.Svg("persona-0001"),
            Is.Not.EqualTo(AbstractArtGenerator.Svg("persona-0002")));

// The palette set is locked at exactly 16 triples.
Assert.That(AbstractArtGenerator.Palettes, Has.Count.EqualTo(16));
```

### `PiHelper` (static class)

| Member | Signature | Description |
|---|---|---|
| `MemoryCheckInterval` | `const int MemoryCheckInterval = 256` | How many decimal places are emitted between memory checks (batched so the GC isn't queried on every digit). |
| `Calculate` | `static PiResult Calculate(int decimalPlaces, double minFreeMemoryFraction = 0.33)` | Computes π to `decimalPlaces` digits after the decimal point (leading `3` always present), stopping early if free system RAM drops below `minFreeMemoryFraction`. Pass `0` to disable the guard and always compute the full count. Throws `ArgumentOutOfRangeException` if `decimalPlaces < 0` or `minFreeMemoryFraction` is outside `[0, 1]`. |
| `PiResult` | `readonly record struct PiResult(string Value, int DecimalPlacesProduced, bool StoppedForMemory, double FreeMemoryFraction)` | The outcome of a `Calculate` run — `Value` is always a correct prefix of π (e.g. `"3.1415..."`, or bare `"3"` for zero places); `StoppedForMemory` is `true` only if the run halted early because free RAM fell below the threshold. |

**How it works:** digits are produced with Gibbons' unbounded spigot algorithm
(`q, r, t, k, n, l` state over `BigInteger`), which streams one *correct* digit per
step without ever needing to know the final length up front — so the digits already
produced are always right, never a half-finished approximation. Because arbitrary-
precision π is a genuine memory hog (the working integers grow roughly linearly with
digit count), the helper reads the system-wide physical-memory load via
`GC.GetGCMemoryInfo()` every `MemoryCheckInterval` digits and aborts once free RAM
drops below `minFreeMemoryFraction` (default 33%). That reading is a recent GC
snapshot, not a live gauge, and reflects the cgroup limit inside a container — it's a
safety valve against OOM/thrashing, not a precise allocator.

```csharp
using MindAttic.Helpers;

// Stream the first 1000 digits of π (stops early if RAM drops below 33%):
var result = PiHelper.Calculate(1000);
Console.WriteLine(result.Value);   // "3.14159265358979..."
Console.WriteLine(result.StoppedForMemory ? "stopped early" : "complete");
```

```csharp
// Zero places gives the bare leading digit.
Assert.That(PiHelper.Calculate(0).Value, Is.EqualTo("3"));

// Four places is the familiar textbook prefix.
Assert.That(PiHelper.Calculate(4).Value, Is.EqualTo("3.1415"));

// Disable the guard entirely to force the full requested count.
var full = PiHelper.Calculate(1000, minFreeMemoryFraction: 0);
Assert.That(full.DecimalPlacesProduced, Is.EqualTo(1000));
```

## How to reference this library

There are no other MindAttic repos consuming `MindAttic.Helpers` yet — this workspace
audit found zero `ProjectReference`/`PackageReference` hits to it outside this repo's
own test project. The two supported ways to pull it in, once a consumer needs it:

**Project reference** (in-workspace, source-level — what
`MindAttic.Helpers.Tests.csproj` does today):

```xml
<ItemGroup>
  <ProjectReference Include="..\MindAttic.Helpers\MindAttic.Helpers\MindAttic.Helpers.csproj" />
</ItemGroup>
```

**NuGet package reference** (once published — the package is configured and ready to
pack/push, see `MindAttic.Helpers/MindAttic.Helpers.csproj`):

```xml
<PackageReference Include="MindAttic.Helpers" Version="1.0.0" />
```

Either way, the API surface is identical:

```csharp
using MindAttic.Helpers;
```

## Build, test, pack

```bash
dotnet build    # library + tests (net10.0, TreatWarningsAsErrors=true, Nullable=enable)
dotnet test     # NUnit suite (16 tests at last verified count)
dotnet pack MindAttic.Helpers/MindAttic.Helpers.csproj -c Release   # produces the NuGet package
```

- Target framework: `net10.0` (see `Directory.Build.props` for shared compiler settings
  — `LangVersion=latest`, `Nullable=enable`, `TreatWarningsAsErrors=true` with
  `CS1591` excluded).
- `MindAttic.Helpers.csproj` has `GenerateDocumentationFile=true`, so every public
  member carries an XML doc comment; the packed NuGet includes this repo's
  `README.md` as `PackageReadmeFile`.
- Versioning is whole-number/major-only per house law: `<Version>1.0.0</Version>`
  today, next release is `2.0.0` (see
  [HOUSE-LAW-1](../MindAttic.HouseRules.md#HOUSE-LAW-1)).

## Directory layout

```
MindAttic.Helpers/
├── MindAttic.Helpers/                     # the library (IsPackable=true)
│   ├── AbstractArtGenerator.cs
│   ├── PiHelper.cs
│   └── MindAttic.Helpers.csproj
├── MindAttic.Helpers.Tests/                # NUnit test project (IsPackable=false)
│   ├── AbstractArtGeneratorTests.cs        # 7 tests
│   ├── PiHelperTests.cs                    # 9 tests
│   └── MindAttic.Helpers.Tests.csproj
├── MindAttic.Helpers.slnx                  # solution stitching both projects
├── Directory.Build.props                   # shared compiler settings for both projects
├── docs/                                   # Codex canon (see below)
│   ├── BIBLE.md
│   ├── AMENDMENTS.md
│   ├── USER_STORIES.md
│   ├── BIBLE.digest.md                     # generated — never hand-edit
│   └── rfc/
├── tools/
│   ├── codex.ps1                           # docs digest/doctor tooling
│   └── build-readme.ps1                    # regenerates README.htm (thin wrapper; see below)
├── CLAUDE.md
├── LICENSE
└── README.md                               # this file
```

## Canonical documentation

This repo follows the MindAttic **Codex** documentation standard — a fact lives in
exactly one layer, linked by stable ID rather than line number:

- **[docs/BIBLE.md](docs/BIBLE.md)** (L0) — what the library is/is not, architecture
  canon, the Laws (`HLP-LAW-1..4`), verified build/test state, glossary.
- **[docs/AMENDMENTS.md](docs/AMENDMENTS.md)** (L1) — append-only change log
  (`HLP-A<n>`); an amendment wins over the bible where they disagree.
- **[docs/USER_STORIES.md](docs/USER_STORIES.md)** (L2) — stories `HLP-US-<Epic><n>`;
  every ✅ names its verifying NUnit test.
- **[docs/rfc/](docs/rfc/)** — design notes that graduate into the bible + stories.
- **[docs/BIBLE.digest.md](docs/BIBLE.digest.md)** — generated by `tools/codex.ps1
  digest`; never hand-edit.
- **[MindAttic.HouseRules.md](../MindAttic.HouseRules.md)** — org-wide laws inherited
  by BIBLE §5.

```powershell
powershell -File tools/codex.ps1 digest   # regenerate docs/BIBLE.digest.md
powershell -File tools/codex.ps1 doctor   # validate front-matter, IDs, refs, stories, digest freshness
```

## Regenerating README.htm

`tools/build-readme.ps1` is a thin wrapper around the shared engine at
`codex-standard/build-readme.ps1` (workspace root) that every MindAttic repo uses, so
all `README.htm` files look and behave identically:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\build-readme.ps1
```

## Laws

- Zero runtime dependencies — BCL only ([HLP-LAW-1](docs/BIBLE.md#HLP-LAW-1)).
- All helpers are pure, deterministic, and static ([HLP-LAW-2](docs/BIBLE.md#HLP-LAW-2)).
- Faithful ports stay bit-for-bit identical to the originals ([HLP-LAW-3](docs/BIBLE.md#HLP-LAW-3)).
- Every helper is locked by tests ([HLP-LAW-4](docs/BIBLE.md#HLP-LAW-4)).
- Whole-number versioning ([HOUSE-LAW-1](../MindAttic.HouseRules.md#HOUSE-LAW-1)).

MIT licensed. Part of the [MindAttic](https://mindattic.com) ecosystem.

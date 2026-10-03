# MindAttic.Helpers

Dependency-free .NET 10 helpers: a deterministic art generator that turns any string into a stable SVG avatar, and a streaming pi digit generator that stops before it runs out of memory.

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](MindAttic.Helpers/MindAttic.Helpers.csproj) [![C#](https://img.shields.io/badge/language-C%23-239120)](MindAttic.Helpers) [![Dependencies](https://img.shields.io/badge/dependencies-BCL%20only-blue)](MindAttic.Helpers/MindAttic.Helpers.csproj) [![Tests](https://img.shields.io/badge/tests-16%20NUnit-brightgreen)](MindAttic.Helpers.Tests) [![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)

```csharp
using MindAttic.Helpers;

// One line: a stable 300x300 portrait for any id, ready for <img src>.
string avatar = AbstractArtGenerator.DataUri("persona-0042");

// Same seed, same picture, on every machine, forever.
bool stable = AbstractArtGenerator.Svg("persona-0042") == AbstractArtGenerator.Svg("persona-0042"); // true

// Pi to 1000 places, halting cleanly if free RAM falls below 33%.
var pi = PiHelper.Calculate(1000);
Console.WriteLine(pi.Value);   // "3.14159265358979..."
```

## Why

- Give every user, project or persona a distinctive picture without storing a single image file.
- Get the identical picture on the server, in the browser and in a build step, because the output depends only on the seed string.
- Drop the result straight into an `<img>` tag or a CSS background as a data URI, with no network calls, fonts or files to ship.
- Compute large numbers of pi digits without risking an out-of-memory crash on a shared machine.
- Add nothing to your dependency graph: the library uses the .NET base class library only.

## Features

### AbstractArtGenerator

- Turns any string seed into a self-contained 300 by 300 SVG: a deep two-stop gradient ground, five to eight translucent circles, rotated rectangles and triangles that bleed past the frame, and one large accent letter.
- Picks from 16 curated palettes, each a gradient start, gradient end and neon accent, reused verbatim from mindattic.com.
- Overlays the first alphanumeric character of the seed (or `?`), or a letter you pass in.
- Returns raw SVG markup or a base64 `data:image/svg+xml` URI.
- Bit-for-bit faithful port of the generative art on mindattic.com: the same FNV-1a hash and Numerical Recipes LCG stream as the original JavaScript.

### PiHelper

- Streams decimal digits of pi with Jeremy Gibbons' unbounded spigot algorithm over `BigInteger` state, so every digit already produced is correct.
- Checks free system memory every 256 digits and stops early once it drops below a threshold you choose (33% by default), reporting that it did.
- Returns a `PiResult` with the digits, the number of places produced, whether it stopped for memory, and the last free-memory reading.

## Quick start

Prerequisites: the .NET 10 SDK.

```powershell
git clone https://github.com/mindattic/MindAttic.Helpers.git
cd MindAttic.Helpers
dotnet build
dotnet test
```

You should see the 16 NUnit tests pass. To use the library from another project, add a project reference (the package is configured for NuGet but not yet published):

```xml
<ItemGroup>
  <ProjectReference Include="..\MindAttic.Helpers\MindAttic.Helpers\MindAttic.Helpers.csproj" />
</ItemGroup>
```

Once a package is published, the reference becomes:

```xml
<PackageReference Include="MindAttic.Helpers" Version="1.0.0" />
```

Either way the namespace is the same:

```csharp
using MindAttic.Helpers;
```

## Usage

### Art in a Razor page

```razor
<img src="@AbstractArtGenerator.DataUri(persona.Id)" alt="@persona.Name" />
```

### Raw SVG with a letter override

Useful when the seed is an opaque slug but you want the display name's initial:

```csharp
string svg = AbstractArtGenerator.Svg("persona-0042", initial: 'M');
```

### Determinism you can test

```csharp
Assert.That(AbstractArtGenerator.Svg("persona-0042"),
            Is.EqualTo(AbstractArtGenerator.Svg("persona-0042")));

Assert.That(AbstractArtGenerator.Svg("persona-0001"),
            Is.Not.EqualTo(AbstractArtGenerator.Svg("persona-0002")));

Assert.That(AbstractArtGenerator.Palettes, Has.Count.EqualTo(16));
```

### Pi digits

```csharp
var result = PiHelper.Calculate(1000);
Console.WriteLine(result.Value);   // "3.14159265358979..."
Console.WriteLine(result.StoppedForMemory ? "stopped early" : "complete");

PiHelper.Calculate(0).Value;   // "3"
PiHelper.Calculate(4).Value;   // "3.1415"

// Disable the guard to always compute the full count.
var full = PiHelper.Calculate(1000, minFreeMemoryFraction: 0);
// full.DecimalPlacesProduced == 1000
```

## API

### AbstractArtGenerator

A static class in `MindAttic.Helpers`.

| Member | Signature | Description |
| --- | --- | --- |
| `Palettes` | `static readonly IReadOnlyList<string[]> Palettes` | The 16 curated colour triples: gradient start, gradient end, accent. |
| `Svg` | `static string Svg(string seed, char? initial = null)` | Raw 300 by 300 SVG markup for the seed. The letter defaults to the seed's first alphanumeric character, or `?`. |
| `DataUri` | `static string DataUri(string seed, char? initial = null)` | The same SVG as a base64 `data:image/svg+xml` URI for an `<img>` source or CSS `background-image`. |

### PiHelper

A static class in `MindAttic.Helpers`.

| Member | Signature | Description |
| --- | --- | --- |
| `MemoryCheckInterval` | `const int MemoryCheckInterval = 256` | Decimal places emitted between memory checks, so the GC is not queried on every digit. |
| `Calculate` | `static PiResult Calculate(int decimalPlaces, double minFreeMemoryFraction = 0.33)` | Pi to the given number of places after the point. Pass `0` as the fraction to disable the guard. Throws `ArgumentOutOfRangeException` for negative places or a fraction outside 0 to 1. |
| `PiResult` | `readonly record struct PiResult(string Value, int DecimalPlacesProduced, bool StoppedForMemory, double FreeMemoryFraction)` | `Value` is always a correct prefix of pi; `StoppedForMemory` is true only when the guard halted the run. |

## How it works

```text
AbstractArtGenerator.Svg("persona-0042")

  seed string --FNV-1a 32-bit--> uint --Numerical Recipes LCG--> stream of doubles
                                                                    |
     palette (1 of 16) <--------------------------------------------+
     gradient angle    <--------------------------------------------+
     5-8 shapes: kind, position, size, colour, opacity, rotation <--+
                                                                    v
  <svg 300x300> gradient rect + shapes + accent letter </svg>   (no I/O, no fonts shipped)
```

The generator's output depends only on the seed, so the same slug yields pixel-identical art anywhere it runs.

PiHelper keeps the spigot's `q, r, t, k, n, l` state as `BigInteger`s and emits one correct digit per step without knowing the final length. Those integers grow roughly linearly with the digit count, so every 256 digits it reads the system-wide memory load from `GC.GetGCMemoryInfo()` and stops once free RAM falls below the threshold. That reading is a recent GC snapshot, not a live gauge, and inside a container it reflects the cgroup limit: it is a safety valve against running out of memory, not a precise allocator.

## Building

```powershell
dotnet build    # library + tests (net10.0, TreatWarningsAsErrors, Nullable enabled)
dotnet test     # 16 NUnit tests
dotnet pack MindAttic.Helpers/MindAttic.Helpers.csproj -c Release   # produces the NuGet package
```

- Target framework: `net10.0`. Shared compiler settings live in `Directory.Build.props`: latest language version, nullable enabled, warnings as errors with `CS1591` excluded.
- The library sets `GenerateDocumentationFile`, so every public member carries an XML doc comment, and the package includes this README.
- Versioning is whole-number and major-only per the MindAttic house rules: `1.0.0` today, `2.0.0` next.

## Testing

| Fixture | Tests | Covers |
| --- | --- | --- |
| `AbstractArtGeneratorTests` | 7 | Determinism, different seeds differ, well-formed SVG with a palette gradient, data URI round-trip, letter override and default, exactly 16 palettes. |
| `PiHelperTests` | 9 | Zero and four places, 99 places against known pi, determinism, longer runs extend shorter ones, guard disabled, impossible threshold stops early, argument validation. |

## Project layout

```text
MindAttic.Helpers/
├── MindAttic.Helpers/                     the library (IsPackable=true)
│   ├── AbstractArtGenerator.cs
│   ├── PiHelper.cs
│   └── MindAttic.Helpers.csproj
├── MindAttic.Helpers.Tests/               NUnit test project (IsPackable=false)
│   ├── AbstractArtGeneratorTests.cs       7 tests
│   ├── PiHelperTests.cs                   9 tests
│   └── MindAttic.Helpers.Tests.csproj
├── MindAttic.Helpers.slnx                 solution stitching both projects
├── Directory.Build.props                  shared compiler settings
├── docs/                                  Codex documentation
├── tools/
│   ├── codex.ps1                          docs digest and doctor tooling
│   └── build-readme.ps1                   regenerates README.htm from this file
├── LICENSE
└── README.md
```

## Laws

- Zero runtime dependencies, base class library only ([HLP-LAW-1](docs/BIBLE.md#HLP-LAW-1)).
- All helpers are pure, deterministic and static ([HLP-LAW-2](docs/BIBLE.md#HLP-LAW-2)).
- Faithful ports stay bit-for-bit identical to the originals ([HLP-LAW-3](docs/BIBLE.md#HLP-LAW-3)).
- Every helper is locked by tests ([HLP-LAW-4](docs/BIBLE.md#HLP-LAW-4)).
- Whole-number versioning (MindAttic house rule HOUSE-LAW-1).

## Limitations

- The package is configured but not yet published to NuGet; reference the project directly for now.
- No other MindAttic repo references this library yet.
- The pi memory guard is a best-effort snapshot and can be disabled; it is not a hard allocation limit.

## Documentation

This repo follows the MindAttic Codex documentation standard: a fact lives in exactly one layer, linked by stable ID.

- [docs/BIBLE.md](docs/BIBLE.md): what the library is and is not, architecture, the Laws, verified build and test state, glossary.
- [docs/AMENDMENTS.md](docs/AMENDMENTS.md): append-only change log; an amendment wins over the bible.
- [User stories](docs/USER_STORIES.md): each completed story names its verifying NUnit test.
- [docs/rfc](docs/rfc): design notes that graduate into the bible and stories.
- [docs/BIBLE.digest.md](docs/BIBLE.digest.md): generated by `tools/codex.ps1 digest`; never hand-edit.
- [AGENTS.md](AGENTS.md): instructions for coding agents working in this repo.

```powershell
powershell -File tools/codex.ps1 digest   # regenerate docs/BIBLE.digest.md
powershell -File tools/codex.ps1 doctor   # validate front-matter, IDs, refs, stories, digest freshness
powershell -NoProfile -ExecutionPolicy Bypass -File tools\build-readme.ps1   # regenerate README.htm
```

## License

MIT. See [LICENSE](LICENSE).

Part of [MindAttic](https://mindattic.com) — see more projects at [github.com/mindattic](https://github.com/mindattic). Related: [MindAttic.Legion](https://github.com/mindattic/MindAttic.Legion), [MindAttic.Vault](https://github.com/mindattic/MindAttic.Vault).

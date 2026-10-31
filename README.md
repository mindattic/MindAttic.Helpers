# MindAttic.Helpers

Dependency-free .NET 10 helpers: a deterministic art generator that turns any string into a stable SVG avatar, a streaming pi digit generator that stops before it runs out of memory, an ASCII diagram aligner so a box's right-hand border never drifts, and a family of ASCII/Braille renderers (bar charts, OHLC candle charts, and Braille-dot-resolution app-mockup wireframes) sharing one dot-packing canvas.

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](MindAttic.Helpers/MindAttic.Helpers.csproj) [![C#](https://img.shields.io/badge/language-C%23-239120)](MindAttic.Helpers) [![Dependencies](https://img.shields.io/badge/dependencies-BCL%20only-blue)](MindAttic.Helpers/MindAttic.Helpers.csproj) [![Tests](https://img.shields.io/badge/tests-80%20NUnit-brightgreen)](MindAttic.Helpers.Tests) [![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)

```csharp
using MindAttic.Helpers;
using MindAttic.Helpers.Ascii;

// One line: a stable 300x300 portrait for any id, ready for <img src>.
string avatar = AbstractArtGenerator.DataUri("persona-0042");

// Same seed, same picture, on every machine, forever.
bool stable = AbstractArtGenerator.Svg("persona-0042") == AbstractArtGenerator.Svg("persona-0042"); // true

// Pi to 1000 places, halting cleanly if free RAM falls below 33%.
var pi = PiHelper.Calculate(1000);
Console.WriteLine(pi.Value);   // "3.14159265358979..."

// A drifting box border, fixed in one call.
string fixedDiagram = AsciiAligner.Align("+-- Stats --+\n| a much longer row |");
```

```powershell
# Or from any shell, via the ascii-align dotnet tool:
ascii-align --check diagram.txt   # exit 1 if misaligned
ascii-align --write diagram.txt   # fix it in place
```

## Why

- Give every user, project or persona a distinctive picture without storing a single image file.
- Get the identical picture on the server, in the browser and in a build step, because the output depends only on the seed string.
- Drop the result straight into an `<img>` tag or a CSS background as a data URI, with no network calls, fonts or files to ship.
- Compute large numbers of pi digits without risking an out-of-memory crash on a shared machine.
- Stop hand-aligning ASCII diagrams by eye — a mechanical pass always gets the right edge straight.
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

### AsciiAligner (`MindAttic.Helpers.Ascii`)

- Pads every line of an ASCII box/table diagram out to its own block's widest line — fixes the right-edge drift that comes from hand-counting characters.
- Extends an existing trailing rule/fill run (`-----+` stays a clean rule) instead of inserting an ugly gap; pads a plain content row with spaces just before its own wall character.
- Treats each blank-line-separated group of lines as its own block, so multiple independent diagrams in one file/string don't bleed into each other.
- Understands common box-drawing characters (`┌─┐│└┘` etc.) as well as plain ASCII (`+-|`).
- Ships with `MindAttic.Helpers.Ascii.Tool`, a `dotnet tool` console wrapper (`ascii-align`) so any shell can validate (`--check`) or fix (`--write`) a diagram file without writing C#.
- Faithful port of `MindAttic.Orchestrator/tools/ascii_align.py` (M.ORCH-T1305), which is retired now that this is the canonical, cross-repo version.

### AsciiCandleChart (`MindAttic.Helpers.Ascii`)

- Draws OHLC bars as a text chart across labeled sessions (e.g. premarket / regular / after-hours per day); the time between sessions isn't drawn, so an overnight gap costs one separator column.
- **Braille mode** (the default) packs 2 time slots x 4 price levels into every character (`⣿`), 8x the detail of one-glyph-per-cell ASCII, via `BrailleCanvas`; volume uses eighth-height blocks (`▁▂▃▄▅▆▇█`). **Candles mode** is pure ASCII (`|` wick, `#` up, `=` down).
- Nice-step price axis, reference lines named at the right edge (`<- Thu close 24.31`), a `last` marker, time ticks, session names and day spans; the bucket size is picked automatically to fit the width.
- Every output line is exactly `Width` characters, so the chart never drifts; deterministic and culture-invariant.

### BrailleCanvas (`MindAttic.Helpers.Ascii`)

- A dot-addressable canvas that packs a 2-wide x 4-tall grid of dots into every character using Unicode Braille Patterns (`U+2800` + dot bits) — 8x the resolution of one glyph per cell, while every line still stays single-width and lines up.
- `SetDot`, `DrawLine` (Bresenham), and `DrawRect` (outline or filled) build up the dot grid; `RenderRows()`/`Render()` emit the text. Out-of-range dots are silently ignored rather than throwing, since chart/mockup code routinely draws right to the edge.
- The shared primitive behind `AsciiCandleChart`'s Braille mode and `AsciiMockup`, extracted so both use one dot-packing implementation instead of two.

### AsciiMockup (`MindAttic.Helpers.Ascii`)

- Renders labeled rectangular panels — an app/UI wireframe — as Braille-dot-resolution box outlines, built entirely on `BrailleCanvas`.
- Each panel's label is centered just inside its top border; every output line is exactly the requested width.
- General-purpose: it draws panels/boxes/labels for any app's screens, not any one app's layout.

## Quick start

Prerequisites: the .NET 10 SDK.

```powershell
git clone https://github.com/mindattic/MindAttic.Helpers.git
cd MindAttic.Helpers
dotnet build
dotnet test
```

You should see the 80 NUnit tests pass (3 pre-existing `AsciiChartTests` BarChart failures — M.HELP-T15/T16/T17/T19 — are tracked separately and unrelated to the rest of the suite). To use the library from another project, add a project reference (the package is configured for NuGet but not yet published):

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

The `ascii-align` CLI isn't published as a global tool yet either; run it from the repo with:

```powershell
dotnet run --project MindAttic.Helpers.Ascii.Tool -- --check path\to\diagram.txt
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

### Aligning an ASCII diagram

```csharp
using MindAttic.Helpers.Ascii;

string text = "+-- Stats --+\n| a much longer row of content |";
Console.WriteLine(AsciiAligner.Align(text));
// +-- Stats ---------------------+
// | a much longer row of content |

AsciiAligner.IsAligned(text);          // false
AsciiAligner.IsAligned(AsciiAligner.Align(text));   // true
```

### Sketching a wireframe

```csharp
using MindAttic.Helpers.Ascii;

var panels = new[]
{
    new MockupPanel(0, 0, 40, 10, "Sidebar"),
    new MockupPanel(40, 0, 40, 10, "Main"),
};
Console.WriteLine(AsciiMockup.Render(panels, width: 80, height: 10, title: "Dashboard"));
```

`AsciiMockup` is built entirely on `BrailleCanvas`, which you can also draw on directly:

```csharp
var canvas = new BrailleCanvas(dotWidth: 20, dotHeight: 8);
canvas.DrawRect(0, 0, 20, 8);
canvas.DrawLine(0, 0, 19, 7);
Console.WriteLine(canvas.Render());
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

### AsciiAligner

A static class in `MindAttic.Helpers.Ascii`.

| Member | Signature | Description |
| --- | --- | --- |
| `Align` | `static string Align(string text)` | Aligns every blank-line-separated block independently; blank lines pass through unchanged. Preserves trailing-newline presence. |
| `IsAligned` | `static bool IsAligned(string text)` | `true` when `Align(text) == text`. |

The `ascii-align` console wrapper (`MindAttic.Helpers.Ascii.Tool`) exposes the same behaviour as a CLI: `ascii-align [path]` (stdin if omitted), `--check` (exit 1 if misaligned), `--write` (rewrite the file in place).

### AsciiChart

A static class in `MindAttic.Helpers.Ascii`.

| Member | Signature | Description |
| --- | --- | --- |
| `ChartPoint` | `readonly record struct ChartPoint(string Label, double Value)` | One labeled value to plot as a bar. |
| `BarChart` | `static string BarChart(IReadOnlyList<ChartPoint> points, int maxBarWidth = 40, double? scaleMax = null, char fillChar = '#', double? markerValue = null, char markerChar = '\|', string? valueFormat = null)` | One bar per line: a right-aligned label, a bar scaled to `maxBarWidth`, and the formatted value. Throws on an empty series or a non-positive `maxBarWidth`. |

### AsciiCandleChart

A static class in `MindAttic.Helpers.Ascii`.

| Member | Signature | Description |
| --- | --- | --- |
| `OhlcBar` | `readonly record struct OhlcBar(DateTime Time, double Open, double High, double Low, double Close, double Volume = 0)` | One open/high/low/close/volume bar. |
| `ChartSession` | `sealed record ChartSession(string Label, DateTime Start, DateTime End, string Group = "")` | A labeled, contiguous stretch of time drawn as one block of columns. |
| `ChartReferenceLine` | `sealed record ChartReferenceLine(string Label, double Value, char LineChar = '-')` | A horizontal reference line, named at the right edge. |
| `CandleChartOptions` | `sealed record CandleChartOptions { ... }` | Layout and glyph options: `Mode` (Braille/Candles), `Width`, `Height`, `VolumeHeight`, `BucketSize`, `ReferenceLines`, and more. |
| `Render` | `static string Render(IReadOnlyList<OhlcBar> bars, IReadOnlyList<ChartSession> sessions, CandleChartOptions? options = null)` | The full chart: price pane, optional volume pane, time/session/day axes. Every line is exactly `options.Width` characters. |

### BrailleCanvas

A sealed class in `MindAttic.Helpers.Ascii` (instance-based: it holds the dot grid being built).

| Member | Signature | Description |
| --- | --- | --- |
| `DotWidth` / `DotHeight` | `int DotWidth { get; }` / `int DotHeight { get; }` | The canvas size in dots. |
| `Columns` / `Rows` | `int Columns { get; }` / `int Rows { get; }` | The rendered size in characters: `ceil(DotWidth / 2)` and `ceil(DotHeight / 4)`. |
| `BrailleCanvas` | `BrailleCanvas(int dotWidth, int dotHeight)` | Creates an empty canvas. Throws `ArgumentOutOfRangeException` for a non-positive width or height. |
| `SetDot` | `void SetDot(int x, int y, bool on = true)` | Turns one dot on or off, in dot coordinates. Out of range is a silent no-op, never a throw. |
| `DrawLine` | `void DrawLine(int x0, int y0, int x1, int y1)` | A straight line of dots (Bresenham), in dot coordinates. |
| `DrawRect` | `void DrawRect(int x, int y, int width, int height, bool filled = false)` | A rectangle of dots; outline only unless `filled` is true. |
| `RenderRows` | `string[] RenderRows()` | One string per output row (`Rows` strings, each `Columns` characters); an empty cell renders as a space. |
| `Render` | `string Render()` | `RenderRows()` joined by `'\n'`. |

### AsciiMockup

A static class in `MindAttic.Helpers.Ascii`.

| Member | Signature | Description |
| --- | --- | --- |
| `MockupPanel` | `readonly record struct MockupPanel(int X, int Y, int Width, int Height, string? Label = null)` | One labeled rectangular region, in character cells. |
| `Render` | `static string Render(IReadOnlyList<MockupPanel> panels, int width, int height, string? title = null)` | A Braille-dot-resolution wireframe: each panel's border plus its centered label. Throws on an empty panel list or a panel that doesn't fit the canvas. |

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

`AsciiAligner.Align` splits the input on blank lines into independent blocks, finds each block's widest (right-trimmed) line, then pads every other line out to that width: a line ending in a rule/fill character (`-=_.~` or box-drawing dashes) gets that character repeated, anything else gets spaces inserted just before its own trailing wall character (`|`, `+`, a box-drawing corner, `>`). No parsing of box structure happens — it's a per-line text operation, so it has no notion of nested boxes or column alignment within a row, only overall line width per block.

## Building

```powershell
dotnet build    # library + tests + ascii-align tool (net10.0, TreatWarningsAsErrors, Nullable enabled)
dotnet test     # 80 NUnit tests
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
| `AsciiAlignerTests` | 7 | Short rows padded to the block's widest, a rule run extends instead of gapping, box-drawing characters, independent blank-line-separated blocks, already-aligned text is a no-op, trailing-newline presence preserved, unbordered text still right-padded. |
| `AsciiChartTests` | 9 (6 passing, 3 pre-existing failures: M.HELP-T15/T16/T17/T19) | Default scaling, label right-align, explicit `scaleMax` headroom, a marker widening the implicit scale, marker overlay position, value formatting, equal line widths, empty-series and non-positive-width validation. |
| `AsciiCandleChartTests` | 26 | Exact requested width in Braille and Candles modes, Candles mode is pure ASCII and bar-order independent, Braille mode uses only Braille/ASCII glyphs, Braille's 4x vertical resolution, session breaks line up in every pane row, auto-bucket picks the smallest fit, bucket aggregation, reference lines drawn and named, session/day labeling, rejects a chart that can't fit, nice-step rounding, compact-clock formatting. |
| `BrailleCanvasTests` | 12 | A lone dot renders the exact Braille character, an empty canvas is all spaces, `RenderRows`/`Render` shape, out-of-range `SetDot` doesn't throw, `DrawRect` outline vs filled, `DrawLine` horizontal/vertical/diagonal, constructor validation. |
| `AsciiMockupTests` | 10 | A single panel's border and centered label, multiple panels render independently, exact line width with and without a title, invalid panels/canvas throw. |

## Project layout

```text
MindAttic.Helpers/
├── MindAttic.Helpers/                     the library (IsPackable=true)
│   ├── AbstractArtGenerator.cs
│   ├── PiHelper.cs
│   ├── Ascii/
│   │   ├── AsciiAligner.cs
│   │   ├── AsciiChart.cs
│   │   ├── AsciiCandleChart.cs
│   │   ├── BrailleCanvas.cs
│   │   └── AsciiMockup.cs
│   └── MindAttic.Helpers.csproj
├── MindAttic.Helpers.Tests/               NUnit test project (IsPackable=false)
│   ├── AbstractArtGeneratorTests.cs       7 tests
│   ├── PiHelperTests.cs                   9 tests
│   ├── AsciiAlignerTests.cs               7 tests
│   ├── AsciiChartTests.cs                 9 tests
│   ├── AsciiCandleChartTests.cs           26 tests
│   ├── BrailleCanvasTests.cs              12 tests
│   ├── AsciiMockupTests.cs                10 tests
│   └── MindAttic.Helpers.Tests.csproj
├── MindAttic.Helpers.Ascii.Tool/          ascii-align dotnet-tool console wrapper
│   ├── Program.cs
│   └── MindAttic.Helpers.Ascii.Tool.csproj
├── MindAttic.Helpers.slnx                 solution stitching all three projects
├── Directory.Build.props                  shared compiler settings
├── docs/                                  Codex documentation
├── tools/
│   ├── codex.ps1                          docs digest and doctor tooling
│   └── build-readme.ps1                   regenerates README.htm from this file
├── LICENSE
└── README.md
```

## Laws

- Zero runtime dependencies, base class library only ([M.HELP-LAW-1](docs/BIBLE.md#M.HELP-LAW-1)).
- All helpers are pure, deterministic and static ([M.HELP-LAW-2](docs/BIBLE.md#M.HELP-LAW-2)).
- Faithful ports stay bit-for-bit identical to the originals ([M.HELP-LAW-3](docs/BIBLE.md#M.HELP-LAW-3)).
- Every helper is locked by tests ([M.HELP-LAW-4](docs/BIBLE.md#M.HELP-LAW-4)).
- Whole-number versioning (MindAttic house rule HOUSE-LAW-1).

## Limitations

- The package is configured but not yet published to NuGet; reference the project directly for now.
- `ascii-align` isn't published as a global `dotnet tool` yet either; run it with `dotnet run --project MindAttic.Helpers.Ascii.Tool` in the meantime.
- No other MindAttic repo references this library yet.
- The pi memory guard is a best-effort snapshot and can be disabled; it is not a hard allocation limit.
- `AsciiAligner` works line-by-line per block; it doesn't parse box structure, so it can't detect or fix misaligned *interior* columns within a multi-column row, only each line's overall width.

## Documentation

This repo follows the MindAttic Codex documentation standard: a fact lives in exactly one layer, linked by stable ID.

- [docs/BIBLE.md](docs/BIBLE.md): what the library is and is not, architecture, the Laws, verified build and test state, glossary.
- [docs/AMENDMENTS.md](docs/AMENDMENTS.md): pending decisions not yet folded into the bible (normally empty).
- [User stories](docs/USER_STORIES.md): each completed story names its verifying NUnit test.
- [docs/rfc](docs/rfc): open design notes; once decided they are folded into the bible and stories and deleted.
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

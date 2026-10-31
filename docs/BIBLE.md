---
codex: 1
project: MindAttic.Helpers
code: M.HELP
layer: bible
status: living
updated: 2026-10-31
---

# MindAttic.Helpers — Project Bible
> Single source of truth for what MindAttic.Helpers IS, is NOT, and the rules that keep it coherent.
> README says how to build/run; this says how to think about the system.

## 1. The one sentence {#M.HELP-§1}
MindAttic.Helpers is a small, zero-dependency .NET library of pure, deterministic
utility helpers shared across the MindAttic ecosystem — each helper is a single
static class with a stable, faithfully-ported behaviour.

## 2. The product promise {#M.HELP-§2}
- **Deterministic by construction.** Given the same input, a helper always returns
  the same output, anywhere it runs (server, client, build step). See
  [`AbstractArtGenerator`](#M.HELP-§4), [`PiHelper`](#M.HELP-§4) and
  [`AsciiAligner`](#M.HELP-§4).
- **Zero runtime dependencies.** The library references only the .NET base class
  library (`System.*`). No NuGet runtime dependencies ship with the package.
- **Drop-in, self-contained.** Each helper does one thing and ships no fonts, files,
  or network calls. `AbstractArtGenerator.DataUri(seed)` is usable directly in an
  `<img src>`; `PiHelper.Calculate(n)` returns a value record, no I/O.
- **Faithful ports.** Where a helper mirrors an existing implementation (e.g. the
  generative art on mindattic.com), it reproduces that behaviour bit-for-bit so the
  output matches across the stack.

## 3. What it is NOT {#M.HELP-§3}
- **NOT an application or service.** The library itself has no host, no DI graph, no
  entry point — it is a class library (`MindAttic.Helpers.csproj`, `IsPackable=true`).
  `MindAttic.Helpers.Ascii.Tool` is a separate, thin `dotnet tool` console wrapper
  around one helper (`AsciiAligner`), not the library gaining an entry point.
- **NOT a grab-bag of stateful utilities.** Helpers are pure static classes with no
  shared mutable state, no singletons, no configuration.
- **NOT a dependency hub.** It must not take on third-party runtime dependencies; a
  helper that needs one belongs in a different package.
- **NOT a UI / rendering toolkit.** `AbstractArtGenerator` emits SVG markup; it does
  not rasterize, lay out, or render anything itself.
- **NOT a precision allocator / memory manager.** `PiHelper`'s memory guard is a
  best-effort safety valve over a recent GC snapshot, not a live or exact gauge.

## 4. Architecture canon {#M.HELP-§4}

```
                 MindAttic.Helpers (net10.0, IsPackable)
                 ┌───────────────────────────────────────────┐
   seed:string ─►│  AbstractArtGenerator (static)            │─► SVG / data URI
                 │    Rng (FNV-1a → LCG, struct)             │
                 │                                            │
   places:int  ─►│  PiHelper (static)                        │─► PiResult (record struct)
                 │    Gibbons unbounded spigot + mem guard   │
                 │                                            │
   text:string ─►│  Ascii.AsciiAligner (static)               │─► aligned text
                 │    per-block right-edge padding            │
                 └───────────────────────────────────────────┘
                     ▲ referenced by ▲            ▲ referenced by ▲
      MindAttic.Helpers.Tests            MindAttic.Helpers.Ascii.Tool
      (NUnit, IsPackable=false)          (console, PackAsTool, "ascii-align")
```

### 4.1 Projects
- `MindAttic.Helpers/MindAttic.Helpers.csproj` — the library. `net10.0`,
  `RootNamespace`/`AssemblyName` = `MindAttic.Helpers`, `IsPackable=true`,
  `GenerateDocumentationFile=true`, MIT, packs `README.md`.
- `MindAttic.Helpers.Tests/MindAttic.Helpers.Tests.csproj` — NUnit test project
  (`IsPackable=false`), references the library.
- `MindAttic.Helpers.Ascii.Tool/MindAttic.Helpers.Ascii.Tool.csproj` — thin console
  wrapper around `AsciiAligner`, packed as a `dotnet tool` (`ToolCommandName=ascii-align`)
  so any shell, in any repo, can validate/fix a diagram without a C# host project.
- `MindAttic.Helpers.slnx` — solution stitching all three projects.

### 4.2 Domain model (NOUNS)
- **Helper** — a `public static` class in namespace `MindAttic.Helpers` (or a nested
  namespace such as `MindAttic.Helpers.Ascii`); pure, deterministic, dependency-free.
- **`AbstractArtGenerator`** — turns a string `seed` into a stable 300×300 SVG
  fingerprint. Holds the 16 curated `Palettes` and a private `Rng` struct.
- **`Rng`** — private struct: FNV-1a (32-bit) seed over UTF-16 code units, advanced
  by a Numerical-Recipes LCG; reproduces the mindattic.com JS stream bit-for-bit.
- **`PiHelper`** — computes decimal digits of π via Gibbons' unbounded spigot, with a
  memory safety valve.
- **`PiHelper.PiResult`** — `readonly record struct` (Value, DecimalPlacesProduced,
  StoppedForMemory, FreeMemoryFraction): the outcome of a `Calculate` run.
- **`Ascii.AsciiAligner`** — pads every line of a blank-line-separated ASCII
  box/table block out to that block's widest line, so a hand-drawn diagram's right
  edge stops drifting. Faithful C# port of `MindAttic.Orchestrator/tools/ascii_align.py`
  (M.ORCH-T1305; that script is retired once this version ships).
- **`Ascii.ChartPoint`** — `readonly record struct` (Label, Value): one labeled value
  plotted by `AsciiChart.BarChart`.
- **`Ascii.AsciiChart`** — renders a labeled series as an ASCII horizontal bar chart:
  bars scaled to a max width, an optional target-line marker, formatted values.
- **`Ascii.OhlcBar`** / **`Ascii.ChartSession`** / **`Ascii.ChartReferenceLine`** — the
  inputs to `AsciiCandleChart`: one open/high/low/close/volume bar, a labeled
  contiguous stretch of time (e.g. a premarket/regular/after-hours session), and a
  horizontal reference line.
- **`Ascii.AsciiCandleChart`** — renders OHLC bars as a text chart across labeled
  sessions (M.HELP-T18): nice-step price axis, optional volume pane, time/session/day
  axes, every line exactly `Width` characters. Braille mode (the default) packs 2 time
  slots x 4 price levels per character via `Ascii.BrailleCanvas`; Candles mode is pure
  ASCII.
- **`Ascii.BrailleCanvas`** — a dot-addressable canvas (M.HELP-T20) that packs a
  2-wide x 4-tall grid of dots into every character using Unicode Braille Patterns
  (`U+2800` + dot bits): `SetDot`, `DrawLine` (Bresenham), `DrawRect` (outline or
  filled), `RenderRows`/`Render`. Extracted from `AsciiCandleChart`'s former private
  bit-packing so it and `AsciiMockup` share one implementation instead of two
  (M.HELP-LAW-1/2); the dot→bit mapping is reused verbatim, so `AsciiCandleChart`'s
  own output is unchanged (M.HELP-LAW-3).
- **`Ascii.MockupPanel`** — `readonly record struct` (X, Y, Width, Height, Label): one
  labeled rectangular region of a wireframe, in character cells.
- **`Ascii.AsciiMockup`** — renders labeled panels as a Braille-dot-resolution app/UI
  wireframe (M.HELP-T20) — 8x the edge detail of a plain box-drawing border — built
  entirely on `Ascii.BrailleCanvas`. General-purpose: it draws panels/boxes/labels for
  any app's screens, not any one app's layout.

### 4.3 Key services (VERBS)
- `AbstractArtGenerator.Svg(seed, initial?)` — raw 300×300 SVG markup.
- `AbstractArtGenerator.DataUri(seed, initial?)` — base64 `data:image/svg+xml` URI.
- `PiHelper.Calculate(decimalPlaces, minFreeMemoryFraction = 0.33)` — produce π to
  `decimalPlaces` places, stopping early if free RAM drops below the fraction.
- `Ascii.AsciiAligner.Align(text)` — the aligned text, one independently-padded block
  per blank-line-separated group.
- `Ascii.AsciiAligner.IsAligned(text)` — `true` when `Align(text) == text`.
- `Ascii.AsciiChart.BarChart(points, maxBarWidth, scaleMax?, fillChar, markerValue?, markerChar, valueFormat?)` —
  one bar per line: right-aligned label, a scaled bar, the formatted value.
- `Ascii.AsciiCandleChart.Render(bars, sessions, options?)` — the full OHLC chart
  across labeled sessions.
- `Ascii.BrailleCanvas.SetDot(x, y, on = true)` / `.DrawLine(x0, y0, x1, y1)` /
  `.DrawRect(x, y, width, height, filled = false)` / `.RenderRows()` / `.Render()` —
  build and render a Braille-dot canvas.
- `Ascii.AsciiMockup.Render(panels, width, height, title?)` — a wireframe of labeled
  panel outlines and centered labels, exactly `width` characters per line.

## 5. The Laws {#M.HELP-§5}
This project **inherits the org-wide House Rules** verbatim — see
[`MindAttic.HouseRules.md`](../../MindAttic.HouseRules.md). Do not restate them here.
The most load-bearing for this library:
- [HOUSE-LAW-1 — Whole-number versioning](../../MindAttic.HouseRules.md#HOUSE-LAW-1)
  (`<Version>1.0.0</Version>` today; next release is `2.0.0`).
- [HOUSE-LAW-8 — Definition of done is verified, not asserted](../../MindAttic.HouseRules.md#HOUSE-LAW-8).
- [HOUSE-LAW-9 — `psst` only on explicit request](../../MindAttic.HouseRules.md#HOUSE-LAW-9).

Project-specific laws:

### M.HELP-LAW-1 — Zero runtime dependencies {#M.HELP-LAW-1}
The library references only the .NET base class library. No third-party runtime
`PackageReference` may be added to `MindAttic.Helpers.csproj`. A helper that needs a
dependency does not belong here. (Test-only dependencies in the Tests project are fine.)

### M.HELP-LAW-2 — Helpers are pure, deterministic, static {#M.HELP-LAW-2}
Every helper is a `public static` class with no shared mutable state and no I/O
(no files, no network). The same input yields the same output everywhere. Any
unavoidable environmental reading (e.g. `PiHelper`'s RAM check) must be a clearly
documented, opt-out best-effort guard — never silent nondeterminism in the result.

### M.HELP-LAW-3 — Faithful ports stay bit-for-bit {#M.HELP-LAW-3}
When a helper ports an existing implementation (e.g. `AbstractArtGenerator` mirrors
mindattic.com's `generateProjectArt`), it must consume its RNG in the same order and
reuse data tables verbatim, so output is identical across the stack. Determinism is
locked by tests; changing the stream is a breaking change (a new whole-number version,
[HOUSE-LAW-1](../../MindAttic.HouseRules.md#HOUSE-LAW-1)) and is recorded here in the bible.

### M.HELP-LAW-4 — Every helper is locked by tests {#M.HELP-LAW-4}
No helper ships without NUnit tests asserting its core invariant (determinism /
correctness), output shape, and edge/error cases. A behaviour is `✅` in
[USER_STORIES](USER_STORIES.md) only when a named test proves it.

## 6. Verified state {#M.HELP-§6}
Evidence re-confirmed **2026-10-31** on `net10.0` (Windows):
- ✅ **Build**: `dotnet build` → Build succeeded. 0 Warning(s), 0 Error(s).
- ⚠️ **Tests**: `dotnet test MindAttic.Helpers.slnx` → **77 passed, 3 failed, 0 skipped** (80 total).
  The 3 failures are pre-existing `AsciiChartTests` BarChart regressions (M.HELP-T15,
  M.HELP-T16, M.HELP-T17; tracked together by M.HELP-T19) — unrelated to, and
  unchanged by, the M.HELP-T20 extraction below.
  - `AbstractArtGeneratorTests` — 7 tests (determinism, distinctness, well-formed
    SVG, data-URI round-trip, initial override + default, 16-palette shape).
  - `PiHelperTests` — 9 tests (bare 3, 3.1415, 99-place reference match, determinism,
    prefix-extension, guard disabled, impossible threshold stops at the interval,
    negative-places + out-of-range-threshold throw).
  - `AsciiAlignerTests` — 7 tests (pads short rows to the block's widest, extends an
    existing rule run instead of inserting a gap, box-drawing characters, independent
    blank-line-separated blocks, already-aligned text is a no-op, trailing-newline
    presence preserved, plain unbordered text still gets right-padded).
  - `AsciiChartTests` — 9 tests, **6 passing / 3 failing** (passing: default scaling,
    an explicit marker widens the scale, value-format applied, every line the same
    width, empty-series and non-positive-width validation; failing — M.HELP-T15/T16/
    T17/T19: label right-align, marker overlay position, explicit-scaleMax headroom).
  - `AsciiCandleChartTests` — 26 tests (exact requested width in both modes, Candles
    mode is pure ASCII and order-independent, Braille mode uses only Braille/ASCII
    glyphs, Braille's 4x vertical resolution, session breaks line up in every pane row,
    auto-bucket picks the smallest fit, bucket aggregation, reference lines drawn and
    named, session/day labeling, rejects a chart that can't fit, nice-step rounding,
    compact-clock formatting).
  - `BrailleCanvasTests` — 12 tests (a lone dot renders the exact Braille character per
    the documented bit mapping, an empty canvas is all spaces, `RenderRows` shape
    matches `Columns`/`Rows`, `Render` joins with newlines, out-of-range `SetDot`
    doesn't throw, `DrawRect` outline vs filled, `DrawLine` horizontal/vertical/
    diagonal, constructor rejects non-positive dot width/height).
  - `AsciiMockupTests` — 10 tests (a single panel renders a border and centered label,
    multiple panels render independently, every line is exactly the requested width
    with and without a title, empty panel list / out-of-bounds panel / negative origin
    / non-positive canvas or panel size all throw).
- ✅ **CLI wrapper**: `ascii-align --check` / default / `--write` smoke-tested against a
  deliberately misaligned file — exits 1 under `--check`, prints the fixed diagram by
  default, and `--write` then `--check` round-trips to exit 0.
- Proven helpers: [`AbstractArtGenerator`](#M.HELP-§4), [`PiHelper`](#M.HELP-§4),
  [`Ascii.AsciiAligner`](#M.HELP-§4), [`Ascii.AsciiCandleChart`](#M.HELP-§4),
  [`Ascii.BrailleCanvas`](#M.HELP-§4), [`Ascii.AsciiMockup`](#M.HELP-§4). `Ascii.AsciiChart`
  is proven except the 3 BarChart regressions above (M.HELP-T15/T16/T17/T19).
- See [USER_STORIES.md](USER_STORIES.md) for per-capability test citations.

## 7. Active frontier {#M.HELP-§7}
- Open RFC: [0001 — Lock zero-dependency + full XML-doc coverage in CI](rfc/0001-example.md)
  (planned; would close M.HELP-US-C1 and M.HELP-US-C2).
- Backlog and partial/planned work live in
  [USER_STORIES.md → Priority backlog](USER_STORIES.md).
- Direction: grow the helper set (each new helper a pure static class with tests),
  keeping M.HELP-LAW-1 (zero deps) intact. Candidate additions are tracked as ⬜ stories.
- `MindAttic.Helpers.Ascii.Tool` is not yet published as a NuGet `dotnet tool` package
  (same limitation as the library itself — see README.md → Limitations); for now
  it is run via `dotnet run --project MindAttic.Helpers.Ascii.Tool`.

## 8. Quality bar {#M.HELP-§8}
A helper is "done" (`✅`) when:
1. It is a pure, dependency-free static class (M.HELP-LAW-1, M.HELP-LAW-2).
2. It builds clean with `GenerateDocumentationFile=true` (XML doc comments on all
   public members — no missing-doc warnings).
3. NUnit tests lock its core invariant, output shape, and error/edge cases
   (M.HELP-LAW-4); `dotnet test` is green.
4. Public API is documented in this BIBLE §4.3 and cited by a story in USER_STORIES.
5. Versioning follows [HOUSE-LAW-1](../../MindAttic.HouseRules.md#HOUSE-LAW-1).

## 9. Glossary {#M.HELP-§9}
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
- **Block** (Ascii) — a run of consecutive non-blank lines in text passed to
  `AsciiAligner`; blank lines delimit blocks and each is aligned independently.
- **Rule/fill run** — a trailing sequence of the same rule character (e.g. `---`) at
  the end of a line; `AsciiAligner` extends this run rather than inserting a gap.

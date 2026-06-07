---
codex: 1
project: MindAttic.Helpers
code: HLP
layer: stories
status: living
updated: 2026-06-07
---

# MindAttic.Helpers — User Stories
> ✅ done (shipped & tested) · 🟡 partial · ⬜ planned · 🗑️ cut. Every ✅ cites the test.

## Epic A — Deterministic abstract art
Helper: [`AbstractArtGenerator`](BIBLE.md#HLP-§4).

- **HLP-US-A1 ✅** As a developer, I can turn any string seed into a stable SVG so the
  same slug always renders the same fingerprint art. *Given a seed, When I call
  `Svg(seed)` (or `DataUri(seed)`) twice, Then I get identical output.*
  *(verified by `Svg_IsDeterministicForTheSameSeed`.)*
- **HLP-US-A2 ✅** As a developer, distinct seeds give me visibly distinct art, so
  different personas/projects don't collide. *Given two different seeds, When I
  generate art, Then the SVGs differ.* *(verified by `DifferentSeeds_ProduceDifferentArt`.)*
- **HLP-US-A3 ✅** As a developer, the output is a well-formed 300×300 SVG with a
  palette gradient and a letter, so I can drop it straight into markup. *Given a seed,
  When I call `Svg`, Then it starts `<svg `, ends `</svg>`, contains a `<linearGradient`,
  the `viewBox="0 0 300 300"`, and a `</text>`.* *(verified by
  `Svg_IsWellFormedAndUsesAPaletteGradient`.)*
- **HLP-US-A4 ✅** As a web developer, I can get a base64 data URI that round-trips to
  the SVG, so I can use it directly in `<img src>`/CSS. *Given a seed, When I call
  `DataUri`, Then it starts `data:image/svg+xml;base64,` and decodes back to `Svg(seed)`.*
  *(verified by `DataUri_IsBase64SvgThatRoundTrips`.)*
- **HLP-US-A5 ✅** As a developer, I can override the overlaid initial letter, so an
  opaque slug can still show a display-name initial. *Given a seed and `initial: 'M'`,
  When I generate, Then the SVG contains `>M</text>`.* *(verified by
  `Initial_OverridesTheOverlaidLetter`.)*
- **HLP-US-A6 ✅** As a developer, the letter defaults to the seed's first
  alphanumeric (or `?` if none), so I get a sensible glyph for free. *Given seed
  `persona-0500`/`---`, When I generate, Then the SVG contains `>P</text>`/`>?</text>`.*
  *(verified by `Initial_DefaultsToFirstAlphanumericOfSeed`.)*
- **HLP-US-A7 ✅** As a maintainer, the curated palette set stays exactly 16 colour
  triples, so the house style is preserved. *Given `Palettes`, When inspected, Then it
  has 16 entries each of length 3.* *(verified by `Palettes_AreSixteenTriples`.)*

## Epic B — π to N places with a memory guard
Helper: [`PiHelper`](BIBLE.md#HLP-§4).

- **HLP-US-B1 ✅** As a developer, requesting 0 places gives me a bare `"3"`, so the
  zero edge is well-defined. *Given `Calculate(0)`, Then Value is `"3"`,
  DecimalPlacesProduced is 0, and it did not stop for memory.* *(verified by
  `Calculate_ZeroPlaces_IsBareThree`.)*
- **HLP-US-B2 ✅** As a developer, I get the correct digits of π for a small request,
  formatted `3.1415…`. *Given `Calculate(4)`, Then Value is `"3.1415"` and 4 places
  were produced.* *(verified by `Calculate_FourPlaces_Is3Point1415`.)*
- **HLP-US-B3 ✅** As a developer, longer requests match a known reference of π, so
  correctness holds at scale. *Given `Calculate(99)`, Then Value equals the 99-place
  reference, 99 places, no memory stop.* *(verified by
  `Calculate_NinetyNinePlaces_MatchesKnownPi`.)*
- **HLP-US-B4 ✅** As a developer, the computation is deterministic. *Given two
  `Calculate(250)` runs, Then their Values are equal.* *(verified by
  `Calculate_IsDeterministic`.)*
- **HLP-US-B5 ✅** As a developer, a longer run extends a shorter one (every π prefix
  is a prefix of a longer π). *Given `Calculate(500)` and `Calculate(200)`, Then the
  500-run starts with the 200-run.* *(verified by `Calculate_LongerRunExtendsTheShorterOne`.)*
- **HLP-US-B6 ✅** As a developer, I can disable the guard to compute the full count.
  *Given `Calculate(1000, minFreeMemoryFraction: 0)`, Then 1000 places, no memory stop.*
  *(verified by `Calculate_GuardDisabled_ProducesEveryRequestedPlace`.)*
- **HLP-US-B7 ✅** As an operator, an impossible memory threshold stops the run early
  (at the check interval) with a still-correct prefix, so a huge request can't OOM the
  box. *Given `Calculate(100_000, minFreeMemoryFraction: 1.0)`, Then it stopped for
  memory at `MemoryCheckInterval` places with a correct π prefix.* *(verified by
  `Calculate_ImpossibleMemoryThreshold_StopsEarly`.)*
- **HLP-US-B8 ✅** As a developer, bad arguments throw, so misuse fails fast. *Given
  `Calculate(-1)` or an out-of-range `minFreeMemoryFraction`, Then
  `ArgumentOutOfRangeException`.* *(verified by `Calculate_RejectsNegativePlaces`,
  `Calculate_RejectsOutOfRangeThreshold`.)*

## Epic C — Library hygiene
- **HLP-US-C1 🟡** As a consumer, the package carries zero third-party runtime
  dependencies (HLP-LAW-1). *Asserted by inspection of `MindAttic.Helpers.csproj`
  (no runtime `PackageReference`); not yet locked by an automated test.*
- **HLP-US-C2 🟡** As a consumer, every public member has XML docs
  (`GenerateDocumentationFile=true`). *Asserted by a clean build with the flag on; not
  yet enforced as a warning-as-error gate.*

## Priority backlog
1. **HLP-US-C1 → ✅**: add a test (or `doctor` check) asserting the library project
   declares no runtime `PackageReference`, to lock HLP-LAW-1.
2. **HLP-US-C2 → ✅**: enable `TreatWarningsAsErrors` (or a doc-coverage check) so
   missing XML docs fail the build.
3. ⬜ Future helpers: each new helper arrives as an Epic with stories that each cite a
   verifying NUnit test before flipping to ✅.

### Audit log
No stories have been changed since creation (initial Codex import, 2026-06-07); the
original spec equals the current text above.

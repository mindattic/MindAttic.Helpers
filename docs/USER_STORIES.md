---
codex: 1
project: MindAttic.Helpers
code: M.HELP
layer: stories
status: living
updated: 2026-10-03
---
<!-- GENERATED from MindAttic.Workflow/data/workflow.db by `workflow db render`. Do not edit: this file is rewritten from the database; change stories and tickets with the workflow CLI. -->

# MindAttic.Helpers — User Stories
> ✅ done (shipped & tested) · 🟡 partial · ⬜ planned. Every ✅ cites the test.

## Epic A — Deterministic abstract art
Helper: [`AbstractArtGenerator`](BIBLE.md#M.HELP-§4).

- **M.HELP-US-A1 ✅** As a developer, I can turn any string seed into a stable SVG so the
  same slug always renders the same fingerprint art. *Given a seed, When I call
  `Svg(seed)` (or `DataUri(seed)`) twice, Then I get identical output.*
  *(verified by `Svg_IsDeterministicForTheSameSeed`.)*
- **M.HELP-US-A2 ✅** As a developer, distinct seeds give me visibly distinct art, so
  different personas/projects don't collide. *Given two different seeds, When I
  generate art, Then the SVGs differ.* *(verified by `DifferentSeeds_ProduceDifferentArt`.)*
- **M.HELP-US-A3 ✅** As a developer, the output is a well-formed 300×300 SVG with a
  palette gradient and a letter, so I can drop it straight into markup. *Given a seed,
  When I call `Svg`, Then it starts `<svg `, ends `</svg>`, contains a `<linearGradient`,
  the `viewBox="0 0 300 300"`, and a `</text>`.* *(verified by
  `Svg_IsWellFormedAndUsesAPaletteGradient`.)*
- **M.HELP-US-A4 ✅** As a web developer, I can get a base64 data URI that round-trips to
  the SVG, so I can use it directly in `<img src>`/CSS. *Given a seed, When I call
  `DataUri`, Then it starts `data:image/svg+xml;base64,` and decodes back to `Svg(seed)`.*
  *(verified by `DataUri_IsBase64SvgThatRoundTrips`.)*
- **M.HELP-US-A5 ✅** As a developer, I can override the overlaid initial letter, so an
  opaque slug can still show a display-name initial. *Given a seed and `initial: 'M'`,
  When I generate, Then the SVG contains `>M</text>`.* *(verified by
  `Initial_OverridesTheOverlaidLetter`.)*
- **M.HELP-US-A6 ✅** As a developer, the letter defaults to the seed's first
  alphanumeric (or `?` if none), so I get a sensible glyph for free. *Given seed
  `persona-0500`/`---`, When I generate, Then the SVG contains `>P</text>`/`>?</text>`.*
  *(verified by `Initial_DefaultsToFirstAlphanumericOfSeed`.)*
- **M.HELP-US-A7 ✅** As a maintainer, the curated palette set stays exactly 16 colour
  triples, so the house style is preserved. *Given `Palettes`, When inspected, Then it
  has 16 entries each of length 3.* *(verified by `Palettes_AreSixteenTriples`.)*

## Epic B — π to N places with a memory guard
Helper: [`PiHelper`](BIBLE.md#M.HELP-§4).

- **M.HELP-US-B1 ✅** As a developer, requesting 0 places gives me a bare `"3"`, so the
  zero edge is well-defined. *Given `Calculate(0)`, Then Value is `"3"`,
  DecimalPlacesProduced is 0, and it did not stop for memory.* *(verified by
  `Calculate_ZeroPlaces_IsBareThree`.)*
- **M.HELP-US-B2 ✅** As a developer, I get the correct digits of π for a small request,
  formatted `3.1415…`. *Given `Calculate(4)`, Then Value is `"3.1415"` and 4 places
  were produced.* *(verified by `Calculate_FourPlaces_Is3Point1415`.)*
- **M.HELP-US-B3 ✅** As a developer, longer requests match a known reference of π, so
  correctness holds at scale. *Given `Calculate(99)`, Then Value equals the 99-place
  reference, 99 places, no memory stop.* *(verified by
  `Calculate_NinetyNinePlaces_MatchesKnownPi`.)*
- **M.HELP-US-B4 ✅** As a developer, the computation is deterministic. *Given two
  `Calculate(250)` runs, Then their Values are equal.* *(verified by
  `Calculate_IsDeterministic`.)*
- **M.HELP-US-B5 ✅** As a developer, a longer run extends a shorter one (every π prefix
  is a prefix of a longer π). *Given `Calculate(500)` and `Calculate(200)`, Then the
  500-run starts with the 200-run.* *(verified by `Calculate_LongerRunExtendsTheShorterOne`.)*
- **M.HELP-US-B6 ✅** As a developer, I can disable the guard to compute the full count.
  *Given `Calculate(1000, minFreeMemoryFraction: 0)`, Then 1000 places, no memory stop.*
  *(verified by `Calculate_GuardDisabled_ProducesEveryRequestedPlace`.)*
- **M.HELP-US-B7 ✅** As an operator, an impossible memory threshold stops the run early
  (at the check interval) with a still-correct prefix, so a huge request can't OOM the
  box. *Given `Calculate(100_000, minFreeMemoryFraction: 1.0)`, Then it stopped for
  memory at `MemoryCheckInterval` places with a correct π prefix.* *(verified by
  `Calculate_ImpossibleMemoryThreshold_StopsEarly`.)*
- **M.HELP-US-B8 ✅** As a developer, bad arguments throw, so misuse fails fast. *Given
  `Calculate(-1)` or an out-of-range `minFreeMemoryFraction`, Then
  `ArgumentOutOfRangeException`.* *(verified by `Calculate_RejectsNegativePlaces`,
  `Calculate_RejectsOutOfRangeThreshold`.)*

## Epic C — Library hygiene
- **M.HELP-US-C1 🟡** As a consumer, the package carries zero third-party runtime
  dependencies (M.HELP-LAW-1). *Asserted by inspection of `MindAttic.Helpers.csproj`
  (no runtime `PackageReference`); not yet locked by an automated test.*
- **M.HELP-US-C2 🟡** As a consumer, every public member has XML docs
  (`GenerateDocumentationFile=true`). *Asserted by a clean build with the flag on; not
  yet enforced as a warning-as-error gate.*

## Priority backlog
1. **M.HELP-US-C1 → ✅**: add a test (or `doctor` check) asserting the library project
   declares no runtime `PackageReference`, to lock M.HELP-LAW-1.
2. **M.HELP-US-C2 → ✅**: enable `TreatWarningsAsErrors` (or a doc-coverage check) so
   missing XML docs fail the build.
3. ⬜ Future helpers: each new helper arrives as an Epic with stories that each cite a
   verifying NUnit test before flipping to ✅.

## Tickets
> Permanent log of every change and fix — this repo's ticket system. Rows are append-only: never
> delete or reword one; only move its Status (open → closed / wont-do) and fill in Ref. Status is
> a word, not a glyph, so tickets don't count toward the story index. Keep this section last.

| ID | Date | Type | Status | Summary | Ref |
|---|---|---|---|---|---|
| M.HELP-T1 | 2026-10-12 | docs | closed | Start the permanent ticket log in USER_STORIES.md | — |
| M.HELP-T2 | 2026-10-15 | chore | closed | Remove duplicate command copies (/deploy, /quickload, /quicksave) now centralized at the user level (~/.claude/commands) | M.FLOW-T14 |
| M.HELP-T3 | 2026-10-16 | chore | closed | install.ps1 adds this repo's 8 .prose/commands stubs for the shared global commands, so any LLM client finds them | mindattic-agent-standard/install.ps1 run 2026-10-16 |
| M.HELP-T4 | 2026-10-16 | chore | open | Bug hunt: run an exhaustive /bughunt over the whole repo and fix every real bug found (each fix under its own fix ticket), then hunt again; close this only when a full pass finds nothing | user request 2026-10-16: approved repo sweep |
| M.HELP-T5 | 2026-10-16 | chore | open | Run the full unit test suite and fix every failure (each under its own fix ticket) until it passes; put the passing run in --ref. No test project: close this wont-do saying so | user request 2026-10-16: approved repo sweep |
| M.HELP-T6 | 2026-10-16 | chore | open | Clean up code and docs: remove dead code, stale comments and outdated or duplicated docs, and simplify where behaviour doesn't change (/simplify); tests stay green | user request 2026-10-16: approved repo sweep |
| M.HELP-T7 | 2026-10-16 | docs | open | Bring README.md up to date with the code as it is now (what it is, setup, usage, commands, current state), following codex-standard/README-STYLE.md; README.htm regenerated if the repo has one | user request 2026-10-16: approved repo sweep |
| M.HELP-T8 | 2026-10-16 | docs | closed | Update USER_STORIES.md | — |

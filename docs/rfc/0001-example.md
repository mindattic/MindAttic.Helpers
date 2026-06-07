---
codex: 1
project: MindAttic.Helpers
code: HLP
layer: rfc
status: planned
updated: 2026-06-07
---

# RFC 0001 — Lock zero-dependency + full XML-doc coverage in CI

## Problem
Two of the library's promises are enforced only by convention today:
HLP-LAW-1 (zero third-party runtime dependencies) and the §8 quality bar's "XML docs
on all public members". Both are asserted by inspection (stories
[HLP-US-C1](../USER_STORIES.md), [HLP-US-C2](../USER_STORIES.md) are 🟡), not proven,
so a future change could regress them silently.

## Options compared
1. **`doctor`/test that parses `MindAttic.Helpers.csproj`** for runtime
   `PackageReference` entries and fails if any appear. Cheap, local, no build change.
2. **`TreatWarningsAsErrors` + `GenerateDocumentationFile`** so missing XML docs (CS1591)
   break the build. Catches doc gaps but not dependency creep.
3. **Both** — a small dependency-guard check plus warnings-as-errors. Fullest coverage.

## Decision
(Planned — not yet adopted.) Lean toward Option 3: add a dependency-guard check and turn
on warnings-as-errors, closing HLP-US-C1 and HLP-US-C2 to ✅ once they are proven.

## What NOT to do
- Do not add any runtime dependency just to implement the guard (would violate the very
  law being protected — HLP-LAW-1).
- Do not silence CS1591 with blanket `#pragma`/`<NoWarn>`; that defeats the purpose.

## Phased plan (with risk)
1. Add the csproj dependency-guard check (low risk; local, no build impact).
2. Flip `TreatWarningsAsErrors=true` (medium risk: may surface pre-existing warnings —
   fix them before merging).
3. Update the stories to ✅ with the new test/check names cited.

## Graduates into:
[BIBLE §5 — HLP-LAW-1](../BIBLE.md#HLP-LAW-1), [BIBLE §8](../BIBLE.md#HLP-§8),
stories [HLP-US-C1 / HLP-US-C2](../USER_STORIES.md).

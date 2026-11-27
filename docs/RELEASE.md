---
layer: release
status: living
---
# MindAttic.Helpers — Release charter

> The one finish line this repo is working toward. Every open ticket in this repo sits under one of the epics
> below, and every epic is part of the milestone. Work that does not fit is out of scope until this milestone is
> reached (owner, 2026-11-27, MAT-T319).

## 1. What this is
Dependency-free .NET 10 helpers: AbstractArtGenerator, PiHelper, AsciiAligner (and the `ascii-align` dotnet
tool), AsciiChart, AsciiCandleChart, BrailleCanvas and AsciiMockup.

## 2. The milestone: MindAttic.Helpers 1.0 {#milestone}
- **Milestone:** MS-53, "MindAttic.Helpers 1.0 published"
- **Published means:** `MindAttic.Helpers.1.0.0.nupkg` and the `ascii-align` tool package on `C:\LocalNuGet`.
- **The natural stopping point:** the helpers are written, tested and documented, but nothing is on the local
  feed, so no app can take them as a package. Packing both makes the library consumable; after that it is frozen
  until a consumer asks for a new helper.

## 3. Done when (acceptance checks)
1. build — `MindAttic.Helpers.slnx` builds clean on main.
2. exists — `C:\LocalNuGet\MindAttic.Helpers.1.0.0.nupkg`.

## 4. Epics, in priority order
| Priority | Epic (ticket) | What it delivers | Features |
|---|---|---|---|
| 900 | M.HELP-T29 1.0 package publication | Library and tool on the local feed | M.HELP-T30 Library package (990: M.HELP-T31), M.HELP-T32 ascii-align tool (980: M.HELP-T33) |

One epic only: the code is finished; publication is the whole remaining gap.

## 5. Out of scope (not before this milestone)
- New helpers without a named consumer.
- Generic cleanups, bughunts and repo-sweep test runs.

## 6. Current verified state
- BarChart test fixes: a9fac33, 50ebb6d, cf43842. BrailleCanvas and AsciiMockup: c87e901.
- README and BIBLE cover every helper. No workspace project references the package today.

## 7. Decision record
- 2026-11-27 — charter drafted from README, BIBLE, git log and tickets; Legion vote: **Approve**, 5 of 9 voters
  answered, plurality quorum reached, consensus 1.0, no dissent.
- Triage: 5 open → done 2 (T19, T7), duplicate 0, wont-do 3 (T5, T6, T28), kept 0, new tasks 2 (T31, T33).

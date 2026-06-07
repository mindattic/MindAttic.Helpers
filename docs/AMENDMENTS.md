---
codex: 1
project: MindAttic.Helpers
code: HLP
layer: amendments
status: living
updated: 2026-06-07
---

# MindAttic.Helpers — Amendments (append-only; amendment wins over the bible)

> Append-only change log. Never rewrite an amendment; supersede it with a new one.
> An amendment overrides the BIBLE where they disagree. Beyond ~25, fold into L0 and
> start a new epoch (note the git tag); history stays in git.

## HLP-A1 — Adopt the Codex documentation standard (supersedes —)
**What changed.** Introduced the Codex canonical-documentation layout for this repo:
`docs/BIBLE.md` (L0), `docs/USER_STORIES.md` (L2), this `docs/AMENDMENTS.md` (L1),
`docs/rfc/` (design notes), the generated `docs/BIBLE.digest.md`, a project `CLAUDE.md`
Codex section, the `tools/codex.ps1` doctor/digest CLI, and the
`.claude/hooks/inject-digest.ps1` SessionStart hook (registered in `.claude/settings.json`).
**Why.** Establish a single source of truth and verified-status discipline across
MindAttic projects, inheriting `MindAttic.HouseRules.md` for org-wide laws.
**Migration.** None — no prior canon docs existed; all Codex files were created fresh.
Existing source code (`AbstractArtGenerator`, `PiHelper`) and `.claude/commands/deploy.md`
were left untouched.

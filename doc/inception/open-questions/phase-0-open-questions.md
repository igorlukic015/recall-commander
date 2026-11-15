# Phase 0 — Open Questions

## OPEN-Q1 — ADOS templates and guides are missing
**Status:** OPEN

`doc/guides/project-inception.md`, `doc/templates/inception-state-template.yaml` and `doc/templates/tribal-knowledge-template.md` don't exist in this repo. The state file and the tribal-knowledge file were built from the bootstrapper agent spec, so they may not match the official templates exactly. Should the ADOS templates and guides be installed (Phase 5 at the latest), with these files reconciled against them afterwards?

### Answer
(The human said to proceed spec-derived for Phase 0 and flag the mismatch. Reconciliation is still open.)

## OPEN-Q2 — Placement of `Docs/` vs `doc/`
**Status:** DEFERRED (resolve by Phase 5)

The existing specs live in `Docs/`, and ADOS uses `doc/`. For Phase 0 the two folders sit side by side. Options:
- (a) keep them side by side, with `doc/00-index.md` linking into `Docs/`;
- (b) move `Docs/` into `doc/` (`doc/overview`, `doc/spec`) as a separate change;
- (c) move it during inception.

Note: on a case-insensitive filesystem (Windows or macOS default), `Docs/` and `doc/` are different names and won't collide, but the near-identical names may confuse contributors.

### Answer
Human: side by side by default for now; it may move later or now. This is recorded as an open decision.

## OPEN-Q3 — Contradiction in the Infrastructure → Application reference
**Status:** DEFERRED (Phase 5)

`AGENTS.md` claims Infrastructure references Application, but the csproj shows it doesn't. Confirm that the code is correct and the doc should be fixed (in the Phase 5 `AGENTS.md` merge).

### Answer
Human (gate 0): deferred to Phase 5.

## OPEN-Q4 — Spaced repetition and scheduling as future direction
**Status:** OPEN (Phase 1)

`Docs/Vision.md` says RC is not a spaced repetition app, yet scheduling and spaced repetition are listed as possible future work. Which statement holds for the north star?

### Answer

## OPEN-Q5 — Supported platforms and distribution
**Status:** OPEN (Phase 3)

Which operating systems are supported (Linux, Windows, macOS)? How is `rc` and the Workbench distributed (`dotnet tool`, self-contained publish, none yet)?

### Answer

## OPEN-Q6 — Tracker
**Status:** DEFERRED (Phase 5)

GitHub Issues or a local markdown backlog in `doc/planning/`?

### Answer
Deferred to Phase 5 by the human.

## OPEN-Q7 — Merge policy for `AGENTS.md` and `.env.example`
**Status:** ANSWERED

### Answer
Merge into the existing files and keep the opencode hard rules. Overwrite only where clearly justified, and tell the user.

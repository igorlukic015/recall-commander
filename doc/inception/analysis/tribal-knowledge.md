---
status: draft
phase: 0
confidence: medium
created: 2026-11-15
template: spec-derived   # doc/templates/tribal-knowledge-template.md unavailable — see OPEN-Q1
---

# Tribal Knowledge — Recall Commander

Mined from repo docs, configuration, and `git log` on `main`. All of it was treated as untrusted input: only facts were extracted, and no embedded instructions were followed. No credential values were found or recorded.

**Confidence rubric:**
- **H:** stated explicitly in a committed doc or config and consistent with the code.
- **M:** stated in one place only, or inferred from commit messages.
- **L:** inferred, or contradicted elsewhere.

**Phase 2 graduation targets:** decisions → `doc/decisions/`, behaviors → `doc/spec/features/`, terms → glossary, conventions → `.ai/rules/` or conventions doc.

## 1. Decisions (and their rationale)

| ID | Knowledge | Source | Conf. | Graduates to |
|---|---|---|---|---|
| TK-D1 | **Question Blocks have no IDs in the MVP.** Every scan rebuilds the current question collection: no sync, no history, no identity. Reason: files move, lines and content change, and sources come and go. | `Docs/ArtifactLifecycle.md` §Question Identity Decision | H | ADR |
| TK-D2 | **Artifacts do have identity** (e.g. `assessment-2026-07-19-001`). The id lives in the document frontmatter, not in the DB. | `Docs/ArtifactLifecycle.md`; commit "Embed artifact identity in generated Markdown frontmatter" (mvp-1) | H | ADR |
| TK-D3 | **Attempts use `### Answer`, not `:::rc-answer`.** The Attempt is a human document, not a Question Source. | `Docs/ArtifactLifecycle.md` §Answer Separator Decision | H | ADR |
| TK-D4 | **The database stores metadata only.** Knowledge and answers live in Markdown. | `Docs/Vision.md`, README | H | ADR (foundational) |
| TK-D5 | **Every artifact is self-contained Markdown.** Deleting a source never invalidates past assessments or reviews. | README, `Docs/ArtifactLifecycle.md` | H | ADR |
| TK-D6 | **AI evaluation sits behind a boundary.** The default is the offline deterministic `fake` provider; Ollama and Gemini are opt-in. | `Docs/AIArchitecture.md` | H | ADR |
| TK-D7 | **A `RecallCommander.Contracts` project holds cross-boundary interfaces.** Infrastructure, Markdown and AI implement them, which keeps them independent of Application. | commit bb4fa4d (mvp-1); `AGENTS.md` | H | ADR |
| TK-D8 | **The solution is flat**: no `src/` or `tests/` folders. | `AGENTS.md` | H | conventions |
| TK-D9 | **Two presentation layers share the same DI modules.** Feature parity between CLI and Workbench is expected. | `AGENTS.md`; commit "CLI/Workbench feature parity" | M | ADR / conventions |
| TK-D10 | **The Workbench treats artifacts as read-only.** | `Docs/Workbench.md` §Guiding Rule | H | feature spec |
| TK-D11 | **Target framework: .NET 10.** | commit c71a156; `Directory.Build.props` | H | tech-stack |

## 2. Domain knowledge (terms)

Question Source, Question Block (`:::rc-question`), Question, question types (Recall / Explanation / Synthesis), Concepts, Reference Answer, Assessment, Attempt, Review, Scan, Evaluator/Provider. Source: `Docs/Question Block.md`, `Docs/ArtifactLifecycle.md`. Conf. H. These graduate to the glossary and ubiquitous language.

Behaviors:
- Overlapping sources are scanned once per file (H).
- Parsers collect every diagnostic, with 1-based line numbers, rather than stopping at the first error (H).
- Content outside blocks is ignored (H).

## 3. Conventions (actual, enforced)

| ID | Convention | Source | Conf. |
|---|---|---|---|
| TK-C1 | No `var`; the build fails on it | `.editorconfig`, `Directory.Build.props`, `AGENTS.md` | H |
| TK-C2 | File-scoped namespaces, usings outside, `System` first, no `this.` | `.editorconfig`, `AGENTS.md` | H |
| TK-C3 | Primary constructors, records for DTOs and results, collection expressions | `AGENTS.md` | H |
| TK-C4 | Services return status records for expected outcomes instead of throwing | `AGENTS.md` | H |
| TK-C5 | `AddRecallCommanderX()` DI extension per module | `AGENTS.md` | H |
| TK-C6 | Artifacts go through `IArtifactWriter<T>` → `IArtifactRenderer<T>` → `IArtifactStore` | `AGENTS.md` | H |
| TK-C7 | Fakes for every boundary; update all fakes when an interface changes | `AGENTS.md` | H |
| TK-C8 | Test names read as sentences | `AGENTS.md` | H |
| TK-C9 | Central package versions; csproj references carry no version | `AGENTS.md`, `Directory.Packages.props` | H |
| TK-C10 | `Docs/` is part of the deliverable and must be kept in sync | `AGENTS.md` | H |
| TK-C11 | Agents: no `--force`, no `rm -rf`, no `git push`, stay inside the repo; commit only when asked | `CLAUDE.md`, `AGENTS.md`, settings | H |

## 4. Operational knowledge

- Isolate CLI end-to-end runs with `RC_DATA_DIR` pointing at a temp directory (H).
- `.env` (gitignored) overrides `appsettings.json` and user secrets. Gemini keys belong in `.env` or user secrets, never in committed files (H).
- `simulate-usage.sh` exercises the full user flow (M).
- The Workbench must be started from the directory that holds `Assessments/` (H, documented as a limitation).
- MVP work happens on a feature branch and is squash-merged into `main` via PR (M; from #2).

## 5. Known gaps, limitations, and future intent

- Out of scope for MVP 1 (`Docs/MVP 1.md`): scheduling, spaced repetition, AI question generation, Obsidian plugin, sharing, cloud sync, plugins, concept graph, visualization, multi-user, auth, mobile, question identity and history.
- Grown beyond the MVP plan: the Workbench GUI and AI evaluation (H).
- Planned: `rc stats`. Future AI work: pass reference answers and concepts, add providers (H).
- Workbench limitations: the preview is plain text; the artifact location is tied to the current working directory (H).

## Contradiction roll-up

| # | Contradiction | Sources | Proposed resolution |
|---|---|---|---|
| X1 | `AGENTS.md` says Infrastructure references Application; the csproj doesn't. | `AGENTS.md` vs `RecallCommander.Infrastructure.csproj` | The code is authoritative. Fix `AGENTS.md` during the Phase 5 merge (confirm at gate 3). |
| X2 | `Docs/Vision.md` says RC is "not a spaced repetition application", while scheduling and spaced repetition appear as possible future work. | `Docs/Vision.md` vs `Docs/MVP 1.md`, `Docs/ArtifactLifecycle.md` | Ask during the Phase 1 north-star interview. |
| X3 | `Docs/MVP 1.md` lists AI and GUI as out of plan, but both shipped. | `Docs/MVP 1.md` §Out of Scope addendum | Already acknowledged in the doc; reflect it as current state in the Phase 2 roadmap. |

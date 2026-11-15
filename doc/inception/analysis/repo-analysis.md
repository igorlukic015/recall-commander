---
status: draft
phase: 0
flow: legacy
confidence: high
created: 2026-11-15
---

# Repo Analysis — Recall Commander

## Profile

- **Type:** a single-repo .NET 10 layered application with two presentation layers: the `rc` CLI and the Avalonia Workbench desktop app.
- **Language:** C# (nullable, implicit usings). 149 tracked `.cs` files: 93 production files and 56 test files.
- **Remote:** GitHub (`origin`). PRs are already in use (`MVP 1 (#2)`).
- **History on `main`:** 7 commits, 2026-07-13 → 2026-11-15, one author. MVP 1 was squash-merged in PR #2, so the 19 granular commits from the `mvp-1` branch are no longer on `main`. The `mvp-1` branch is still present and has the same tree.

## Solution layout (flat, `RecallCommander.slnx`)

| Project | References (csproj) |
|---|---|
| Domain | none |
| Contracts | Domain |
| Application | Contracts, Domain |
| Infrastructure | Contracts, Domain |
| Markdown | Contracts, Domain |
| AI | Contracts, Domain |
| Cli | AI, Application, Contracts, Infrastructure, Markdown |
| Workbench | AI, Application, Contracts, Infrastructure, Markdown |
| Domain.Tests, Application.Tests, Markdown.Tests, AI.Tests, Workbench.Tests, IntegrationTests | xUnit test projects |

**Observed drift:** `AGENTS.md` says Infrastructure references Application for `ArtifactFileNameGenerator`. The csproj shows no such reference: Infrastructure references only Contracts and Domain. This needs reconciling in Phase 3/5. The code wasn't changed.

## Functional surface (implemented)

`rc init`, `rc source add|remove|list`, `rc scan`, `rc assessment create [--count]`, `rc assessment list`, `rc attempt validate`, `rc review create`. The Workbench drives the same workflow through the same DI modules. Planned but not implemented: `rc stats`.

## Key dependencies (central package management)

- Avalonia 12.1.0 and CommunityToolkit.Mvvm 8.4.2
- Spectre.Console(.Cli) 0.49.1
- Markdig 0.38.0
- YamlDotNet 16.2.0
- Microsoft.Data.Sqlite 10.0.10 and Dapper 2.1.35
- Microsoft.Extensions.* 10.0.10 (Configuration, DI, Http, UserSecrets)
- xUnit 2.9.2

## External integrations

- **Ollama:** local HTTP. The endpoint is configurable.
- **Gemini:** Google AI REST API. The API key comes from `.env` or user secrets, and `.env` is gitignored.
- **Default provider:** `fake`, which is offline and deterministic.

## Persistence

- SQLite stores metadata only. Its location is configurable via `RC_DATA_DIR`.
- User-owned Markdown files are the source of truth.
- Generated artifacts are written to an `Assessments/` folder under the working directory.

## Quality baseline (current)

- `TreatWarningsAsErrors` and `EnforceCodeStyleInBuild` are on, so `.editorconfig` violations at warning severity break the build. Example: the ban on `var`.
- Tests: six xUnit projects. Boundaries are faked. IntegrationTests contains both an Integration suite and a CLI EndToEnd suite.
- No CI workflow, coverage gate, or formatting check in CI.
- Agent tooling: opencode (`AGENTS.md`, `opencode.json`, `.opencode/`) and Claude (`CLAUDE.md`, `.claude/settings.json`) share the same hard rules.

## Low-confidence or to-confirm areas

- Whether `Assessments/` in the current working directory is the intended long-term output location (the Workbench limitation depends on it).
- Cross-platform support target (Avalonia allows Win/macOS/Linux; nothing documents which are supported).
- Distribution and packaging: no release or publish process found.
- The Infrastructure → Application reference drift described above.

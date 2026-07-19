# Recall Commander — Agent Instructions

Instructions for AI coding agents (opencode) working in this repository.
opencode loads this file automatically. Follow it exactly.

## Hard rules (never violate)

These are also enforced as `deny` permissions in `opencode.json`.

- **Never use `--force`** or `--force-with-lease` with any command — no force
  pushes, force checkouts, or force flags of any kind.
- **Never run `rm -rf`** / `rm -fr` or any recursive force delete.
- **Never run `git push`.** Committing is fine *when asked*; the human pushes
  manually.
- **Never touch anything outside the project root.** Do not read, write, or
  modify files outside this repository. Use a temp directory for scratch work.

## What this project is

Recall Commander is a **Markdown-first knowledge assessment platform**: it
scans user-owned Markdown files for embedded Question Blocks, generates
assessments, and reviews the user's answers — all as portable Markdown
artifacts. The database stores metadata only; **knowledge belongs to the
user, assessment belongs to Recall Commander.**

Full philosophy and specs live in `Docs/` (see the index at the bottom).
Read the relevant doc before changing behavior in that area.

## Tech stack

- **C# / .NET 10** (`net10.0`), nullable + implicit usings enabled.
- **SQLite** (Microsoft.Data.Sqlite + Dapper) for metadata only.
- **Markdig** (parsing), **YamlDotNet** (frontmatter), **Spectre.Console** (CLI).
- **Avalonia + CommunityToolkit.Mvvm** for the desktop Workbench.
- **xUnit** for all tests.

## Solution layout (flat)

All projects live directly in the repository root next to `RecallCommander.slnx`.
There are **no `src/` or `tests/` folders** — keep it flat.

```
RecallCommander.Domain          Core entities + invariants (no external deps)
RecallCommander.Contracts       Cross-boundary interfaces + models (depends on Domain)
RecallCommander.Application     Use-case services (depends on Contracts)
RecallCommander.Infrastructure  SQLite, filesystem, artifact storage, .env config
RecallCommander.Markdown        Question Block parsing + artifact rendering
RecallCommander.AI              AI evaluation providers (Ollama, Gemini)
RecallCommander.Cli             The `rc` command-line app (Spectre.Console)
RecallCommander.Workbench       Avalonia desktop app (dual-pane UI)
*.Tests / RecallCommander.IntegrationTests   xUnit test projects
```

**Dependency direction flows inward:** Cli/Workbench → Application → Contracts
→ Domain. Infrastructure, Markdown and AI implement Contracts interfaces.
Infrastructure additionally references Application for one shared helper
(`ArtifactFileNameGenerator`, which lives in Contracts). **Never make
Infrastructure/Markdown/AI depend on Application otherwise, and never make
Domain depend on anything.**

Central package management: package versions live in
`Directory.Packages.props`; add `<PackageReference Include="X" />` **without a
version** in the csproj and pin the version in `Directory.Packages.props`.
Shared build settings live in `Directory.Build.props`.

## Build / test / run

```bash
dotnet build                                   # build the whole solution
dotnet test                                    # run all test projects
dotnet run --project RecallCommander.Cli -- <args>   # run the `rc` CLI
dotnet run --project RecallCommander.Workbench       # run the desktop app
```

- The CLI assembly is named **`rc`**. Example: `dotnet run --project
  RecallCommander.Cli -- scan`.
- Set **`RC_DATA_DIR`** to a temp directory to isolate the SQLite store when
  testing the CLI end-to-end (keeps your real workspace untouched).
- **Always build and run the tests before considering a change done.**
  Convenience commands: `/build`, `/test`, `/check`, `/run`.

## Code conventions — the build enforces these

`Directory.Build.props` sets `TreatWarningsAsErrors=true` and
`EnforceCodeStyleInBuild=true`. That means **`.editorconfig` style rules with
`warning` severity are hard build errors.** A style slip fails `dotnet build`.
Match the surrounding code and specifically:

- **Never use `var`.** Always spell out the explicit type. (This is the most
  common way to accidentally break the build.)
- **File-scoped namespaces** (`namespace X;`), `using` directives **outside**
  the namespace, `System` directives sorted first.
- **No `this.` qualification** on member access.
- **Primary constructors** for services and records; **records** for DTOs and
  result types; **collection expressions** (`[]`, `[.. items]`) over
  `new List<T>()`.
- Expression-bodied members when they fit on one line; block bodies otherwise.
- Private instance fields `_camelCase`; private static fields `PascalCase`;
  interfaces start with `I`.
- Prefer switch expressions and pattern matching.

`dotnet format` can auto-apply most of this, but it is slow (whole solution);
usually it is faster to just write it correctly and let the build verify.

### Design patterns already established (reuse them)

- Application services return **status records** (e.g. `AddSourceResult`,
  `CreateReviewResult`) for expected outcomes instead of throwing.
- Parsers **never stop at the first error**: collect `ParseDiagnostic`s (with
  1-based line numbers) and report them all.
- Each module exposes a `ServiceCollectionExtensions.AddRecallCommanderX()`;
  both the CLI and Workbench compose the same modules via DI.
- Artifacts are written through `IArtifactWriter<T>` → `IArtifactRenderer<T>`
  → `IArtifactStore`; a new artifact type only needs a renderer.
- Boundaries are faked in tests (in-memory filesystem, repository, evaluator).
  When you add an interface method, update **all** fakes that implement it
  (`*.Tests/**/Fakes*.cs`, `IntegrationTests/Support/TestBoundaries.cs`).

## Testing

- xUnit across six test projects: Domain, Application, Markdown, AI, Workbench,
  and IntegrationTests (which has `Integration/` and CLI `EndToEnd/` suites).
- Add tests for every behavior change, at the lowest layer that covers it, and
  add a CLI end-to-end / integration test when a change is user-visible.
- Test names read as sentences (e.g. `Removes_a_registered_source`).

## Git & workflow

- **Commit only when asked.** If on the default branch (`main`), create a
  branch first. **Never push** — that's the human's job.
- Use the `gh` CLI for any GitHub operations.
- Do the work, don't just describe it: run the build and tests, fix failures,
  and report outcomes honestly (if tests fail, say so).

## Documentation index (read before changing that area)

| Doc | Contents |
|---|---|
| `Docs/Vision.md` | Philosophy, goals, what RC deliberately is *not* |
| `Docs/MVP 1.md` | Scope, stack, CLI commands, success criteria |
| `Docs/Architecture.md` | Solution layout, layers, data flow, testing strategy |
| `Docs/ArtifactLifecycle.md` | Question Block → Question → Assessment → Attempt → Review, and exact file formats |
| `Docs/Question Block.md` | The `:::rc-question` syntax |
| `Docs/CLI.md` | Every `rc` command, exit codes, storage, configuration |
| `Docs/AIArchitecture.md` | The AI evaluation boundary (fake/Ollama/Gemini) |
| `Docs/Workbench.md` | The Avalonia desktop app |
| `Docs/Examples/` | Sample question, assessment, attempt, review files |

Keep the docs in sync when you change behavior — this project treats `Docs/`
as part of the deliverable.

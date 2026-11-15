---
status: draft
phase: 0
confidence: high
created: 2026-11-15
---

# Material Inventory — Recall Commander

`doc/inception/inputs/` was empty; no extra materials were provided. Everything below comes from the repository at `main` (666be55, "MVP 1 (#2)").

## Product and specification docs (`Docs/`)

| Path | Contents | Feeds phase |
|---|---|---|
| `Docs/Vision.md` | Philosophy (ownership, Markdown-first, metadata-only DB, assessment over memorization), goals, what RC is NOT | 1 (north star) |
| `Docs/MVP 1.md` | MVP 1 objective, out-of-scope list, stack, workflow, modules, domain model, CLI commands, success criteria | 1, 2 |
| `Docs/Architecture.md` | Solution layout, layering, dependency direction, data flow per command, DB, DI, testing strategy, future extensions | 3, 4 |
| `Docs/ArtifactLifecycle.md` | Question Block → Question → Assessment → Attempt → Review; file formats; recorded decisions (question identity, answer separator) | 2, 4 (glossary) |
| `Docs/Question Block.md` | `:::rc-question` syntax, types (recall/explanation/synthesis), parsing and error rules | 1 (spec seeds), 4 |
| `Docs/CLI.md` | Every `rc` command, exit codes, storage, configuration, planned commands | 1 (spec seeds), 3 |
| `Docs/AIArchitecture.md` | AI evaluation boundary, fake/Ollama/Gemini providers, prompts, parsing, error handling | 3 |
| `Docs/Workbench.md` | Avalonia dual-pane desktop app, read-only artifacts rule, limitations | 2 (UI), 4 (ux-guidance) |
| `Docs/Examples/*.md` (6) | Sample question source, assessment, attempt, review | 1 (spec seeds) |

## Repo-level docs and configuration

| Path | Contents |
|---|---|
| `README.md` | Product summary, status, command list, doc index, stack |
| `AGENTS.md` | Hand-written opencode agent instructions: hard rules, layout, conventions, patterns, testing |
| `CLAUDE.md` | Claude hard rules (no force, no rm -rf, no push, stay in repo) |
| `opencode.json`, `.opencode/` | opencode permissions, commands (build/test/check/run), review agent |
| `.claude/settings.json` | Deny rules mirroring the hard rules |
| `.editorconfig` | C# style rules; enforced at build time |
| `Directory.Build.props`, `Directory.Packages.props` | net10.0, nullable, warnings-as-errors, code style in build; central package versions |
| `.env.example` | AI provider config template (keys documented, no values) |
| `RecallCommander.Cli/appsettings.json` | Default AI config (provider `fake`) |
| `simulate-usage.sh` | End-to-end user-flow simulation script |
| `LICENSE` | MIT |
| `Assets/` | Banner and logo images |

## Local-only material (not committed)

- `CodeReview.md`: gitignored ("Local review files"). It was present in the working tree earlier in the session and is no longer there. It isn't on `main`, so it isn't treated as a source of truth.
- `.serena/`, `.claude/.headroom_*`, `RecallCommander.sln.DotSettings.user`, `.idea/`: tool and IDE state; not product material.

## Gaps

- No CI (`.github/` absent).
- No decision records directory; decisions are embedded inline in `Docs/` prose.
- No tracker or backlog artifacts.
- No ADOS guides or templates in the repo (see OPEN-Q1).

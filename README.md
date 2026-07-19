![Recall Commander banner](Assets/banner.png)

# Recall Commander

Recall Commander is a **Markdown-first knowledge assessment platform** designed to evaluate understanding rather than simply test memorization.

Users keep their knowledge in plain Markdown files, organized however they like. Recall Commander scans those files for embedded **Question Blocks**, generates assessment sessions, and produces reviews of how well the user actually understands the material — all as portable, human-readable Markdown artifacts.

> Knowledge belongs to the user. Assessment belongs to Recall Commander.

## Status

Recall Commander is under active development. The `Docs/` folder contains the full specification for **MVP 1**.

The complete assessment workflow is implemented end-to-end:

```
rc init                        # initialize the metadata store (SQLite)
rc source add <path>           # register a directory of Markdown question files
rc source remove <path>        # unregister a source (never touches the directory)
rc source list                 # list registered question sources
rc scan                        # discover Question Blocks across all sources
rc assessment create           # generate an assessment (default: 10 questions)
rc assessment create --count 5 # generate an assessment with 5 questions
rc assessment list             # list generated assessments, newest first
rc attempt validate <file>     # check a completed assessment before review
rc review create <file>        # evaluate an attempt and write a review artifact
```

Answer evaluation runs offline by default (a deterministic evaluator) and can be switched to a local **Ollama** model or the **Gemini** API through configuration — see [`Docs/AIArchitecture.md`](Docs/AIArchitecture.md).

A desktop companion, the **Workbench** (Avalonia, dual-pane Total Commander-style UI), drives the same workflow — see [`Docs/Workbench.md`](Docs/Workbench.md).

Build and run with the .NET SDK:

```
dotnet build
dotnet test
dotnet run --project RecallCommander.Cli -- scan
```

## Core Philosophy

- **Ownership** — users own their knowledge; Recall Commander never locks it into a proprietary format.
- **Markdown first** — every artifact (questions, assessments, attempts, reviews) is plain Markdown, readable years later without the tool.
- **Metadata only in the database** — SQLite stores indexes and history, never the knowledge or answers themselves.
- **Assessment over memorization** — questions range from simple recall to synthesis across concepts, evaluating real understanding.

See [`Docs/Vision.md`](Docs/Vision.md) for the full philosophy.

## How It Works

```
Question Sources          (User owned Markdown files)
        |
        v
Question Blocks           (User authored, embedded in Markdown)
        |
        v
Assessment                (RC generated snapshot of questions)
        |
        v
Attempt                   (User completed assessment)
        |
        v
Review                    (RC evaluation artifact)
```

1. The user writes Markdown notes containing Question Blocks, anywhere they like — an Obsidian vault, a folder of notes, a dedicated question repo.
2. `rc scan` recursively discovers those blocks across configured Question Sources.
3. `rc assessment create` generates an Assessment Markdown file from discovered questions.
4. The user answers by copying the assessment into an Attempt file, in any Markdown editor.
5. `rc review create` evaluates the Attempt and generates a Review Markdown file with feedback and scoring.

Every stage produces an independent, self-contained Markdown artifact — deleting or editing the original source never invalidates past assessments or reviews.

## Documentation

| Doc | Contents |
|---|---|
| [`Docs/Vision.md`](Docs/Vision.md) | Project philosophy, goals, and what Recall Commander deliberately is *not* |
| [`Docs/MVP 1.md`](Docs/MVP%201.md) | Scope, tech stack, CLI commands, and success criteria for the first release |
| [`Docs/Architecture.md`](Docs/Architecture.md) | Solution layout, layer responsibilities, data flow, and testing strategy |
| [`Docs/ArtifactLifecycle.md`](Docs/ArtifactLifecycle.md) | The Question Block → Question → Assessment → Attempt → Review lifecycle and file formats |
| [`Docs/Question Block.md`](Docs/Question%20Block.md) | The `:::rc-question` Question Block syntax embedded in Markdown source files |
| [`Docs/CLI.md`](Docs/CLI.md) | Every `rc` command, exit codes, storage locations, and configuration |
| [`Docs/AIArchitecture.md`](Docs/AIArchitecture.md) | The AI evaluation boundary and the fake/Ollama/Gemini providers |
| [`Docs/Workbench.md`](Docs/Workbench.md) | The Avalonia dual-pane desktop app |
| [`Docs/Examples/`](Docs/Examples/) | Sample question, assessment, attempt, and review files |

## Tech Stack (MVP 1)

- **C# / .NET 10**
- **SQLite** (with **Dapper**) for metadata and history (not source of truth)
- **Markdig** for Markdown parsing, **YamlDotNet** for frontmatter, **Spectre.Console** for the CLI
- **Avalonia** + **CommunityToolkit.Mvvm** for the Workbench desktop app
- **xUnit** for testing

See [`Docs/MVP 1.md`](Docs/MVP%201.md) for details.

## License

MIT — see [LICENSE](LICENSE).

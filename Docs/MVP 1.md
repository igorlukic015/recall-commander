# Recall Commander - MVP 1

## Objective

The objective of MVP 1 is to build the smallest fully functional version of Recall Commander capable of assessing user knowledge from Markdown Question Blocks.

MVP 1 is complete when a user can:

1. Create Markdown documents containing Recall Commander Question Blocks.
2. Configure one or more Question Sources.
3. Scan Question Sources and discover questions.
4. Generate an Assessment Markdown file.
5. Answer questions by editing a copy of the Assessment.
6. Submit the completed Attempt Markdown file.
7. Generate a Review Markdown file.
8. Persist assessment metadata and history.

The goal is to validate the complete assessment workflow from beginning to end.

**Status: the complete workflow (steps 1–7) is implemented and covered by
tests.** Step 8 is intentionally minimal: only question sources and
application settings are persisted so far; artifact metadata tables have not
been needed yet because the Markdown files themselves are the record.

---

# Out of Scope

The following features are intentionally excluded from MVP 1:

* Adaptive scheduling
* Spaced repetition
* AI-generated questions
* Obsidian plugin
* Community sharing
* Cloud synchronization
* Plugin system
* Concept dependency graph
* Knowledge visualization
* Multi-user support
* Authentication
* Mobile applications
* Question identity tracking
* Question history synchronization

These features may or may not be introduced in future versions.

Two things have grown beyond the original MVP plan:

* A desktop GUI (the Avalonia **Workbench**, see [Workbench.md](Workbench.md))
  exists alongside the CLI.
* AI answer evaluation (Ollama and Gemini providers, see
  [AIArchitecture.md](AIArchitecture.md)) is implemented behind the
  evaluation boundary; the offline deterministic evaluator remains the
  default.

---

# Technology Stack

## Language

* C# / .NET 10

## Database

* SQLite (via Microsoft.Data.Sqlite + Dapper)

Purpose:

SQLite stores metadata and application history.

SQLite is **not the source of truth for questions**.

Knowledge and assessment artifacts remain in Markdown files.

## Libraries

* Markdig — Markdown parsing (custom `:::` container parser)
* YamlDotNet — frontmatter and Question Block metadata
* Spectre.Console / Spectre.Console.Cli — the `rc` CLI
* Dapper — persistence (chosen over Entity Framework Core)
* Avalonia + CommunityToolkit.Mvvm — the Workbench desktop app

## Testing

* xUnit (unit, integration and CLI end-to-end test projects)

---

# High-Level Workflow

```text
Markdown Question Sources
        │
        ▼
Markdown Scanner
        │
        ▼
Question Block Parser
        │
        ▼
In-Memory Question Collection
        │
        ▼
Assessment Generator
        │
        ▼
Assessment Markdown
        │
(User creates Attempt by answering)
        │
        ▼
Attempt Markdown
        │
        ▼
Review Generator
        │
        ▼
Review Markdown
        │
        ▼
Metadata Persistence

```

# User Workflow

The commands below are summarized here; [CLI.md](CLI.md) documents each one
in detail.

## Step 1

Initialize Recall Commander.

```bash
rc init
```

Creates the metadata database in the per-user application data directory:

```text
~/.local/share/RecallCommander/recall-commander.db      (Linux)
```

The `RC_DATA_DIR` environment variable overrides the location. `rc init` is
idempotent.

---

## Step 2

Register Question Sources.

Examples:

```bash
rc source add ~/Obsidian

rc source add ~/ProgrammingQuestions

rc source add ~/Biology
```

Multiple Question Sources are supported.

A Question Source is any directory containing Markdown files.

---

## Step 3

Scan Question Sources.

```bash
rc scan
```

Recall Commander:

* recursively scans configured sources
* discovers Markdown documents
* finds Question Blocks
* parses valid questions
* reports invalid blocks without stopping the scan

Example:

```text
Scanning...

CSharp.md
  Found 34 questions

Databases.md
  Found 21 questions

Warnings:

CSharp.md:420
Missing rc-prompt block.

Scan completed.

Questions discovered: 55
```

Questions are not stored in SQLite.

They exist as parsed objects during execution.

---

## Step 4

Generate an assessment.

```bash
rc assessment create            # 10 questions (default)
rc assessment create --count 5
```

Produces, under the current working directory:

```text
Assessments/

    assessment-2026-07-19-001.md
```

The assessment contains:

* frontmatter (type, id, title, created, question count)
* a title and instruction line
* unanswered questions, each ending with an empty `### Answer` heading

Questions are copied into the assessment.

The assessment is independent from the original Question Sources.

---

## Step 5

Answer questions.

The user creates an Attempt by saving a copy of the Assessment file and
filling in answers under each `### Answer` heading.

Example:

```text
Assessments/

    assessment-2026-07-19-001.md

Attempts/

    assessment-2026-07-19-001.attempt.md
```

Recall Commander is not involved during editing.

The user may use:

* Obsidian
* VS Code
* Rider
* Vim
* any Markdown editor

The attempt can be checked before review:

```bash
rc attempt validate Attempts/assessment-2026-07-19-001.attempt.md
```

---

## Step 6

Review the completed Attempt.

```bash
rc review create Attempts/assessment-2026-07-19-001.attempt.md
```

Recall Commander:

* parses answers
* evaluates responses (deterministic evaluator by default; Ollama or Gemini
  when configured)
* generates the review artifact

Example output, under the current working directory:

```text
Reviews/

    review-2026-07-19-001.md
```

---

# Core Modules

The implementation of these modules is described project-by-project in
[Architecture.md](Architecture.md).

## Configuration

Responsibilities:

* application settings
* Question Sources
* configuration loading (appsettings.json, user secrets, `.env`)

---

## Markdown Scanner

Responsibilities:

* recursively scan Question Sources
* discover Markdown documents
* deduplicate files reachable from overlapping sources

---

## Question Block Parser

Responsibilities:

* parse Question Blocks
* validate syntax
* produce Question objects
* report diagnostics with file and line number

---

## Assessment Generator

Responsibilities:

* select questions (random, without duplicates)
* create Assessment Markdown files

---

## Attempt Parser

Responsibilities:

* parse completed Assessment files
* extract user answers
* reject broken documents with full diagnostics

---

## Review Generator

Responsibilities:

* create Review Markdown files
* attach feedback to answers

Evaluation runs behind `IQuestionEvaluator`: a deterministic fake by
default, LLM evaluation (Ollama, Gemini) when configured.

---

## Persistence

Responsibilities:

SQLite currently stores:

* Question Source configuration
* application settings (schema version)

Planned but not yet needed:

* assessment metadata
* attempt metadata
* review metadata

SQLite does not store:

* questions
* answers
* knowledge content

---

# Core Domain Model

## Question

Represents a parsed Question Block.

Fields:

* Question Type (Recall, Explanation, Synthesis)
* Prompt
* Reference Answer (optional)
* Concepts

Questions are temporary parsed objects.

They do not have persistent IDs in MVP 1.

---

## Assessment

Represents a generated assessment artifact.

Contains:

* Title
* Creation Date
* Questions (prompt-only snapshots)

Stored as Markdown.

---

## Attempt

Represents a completed assessment.

Contains:

* Title, creation date and the originating assessment's artifact id
* Assessment questions
* User answers (empty answer = unanswered)

Stored as Markdown, authored by the user.

---

## Review

Represents evaluation of an Attempt.

Contains per question:

* Score (0–10)
* Understanding level (Poor … Excellent)
* Summary
* Strengths
* Missing information
* Incorrect statements
* Suggestions

Plus an overall summary and the evaluator identity.

Stored as Markdown.

---

# CLI Commands

Implemented:

```text
rc init

rc source add <path>

rc source list

rc scan

rc assessment create [--count <n>]

rc assessment list

rc attempt validate <file>

rc review create <file>
```

Potential future commands:

```text
rc source remove

rc stats
```

---

# Success Criteria

MVP 1 is complete when:

```text
Write Markdown
        │
        ▼
Embed Recall Commander Question Blocks
        │
        ▼
Configure Question Source
        │
        ▼
rc scan
        │
        ▼
Questions discovered
        │
        ▼
rc assessment create
        │
        ▼
Assessment Markdown generated
        │
        ▼
User creates Attempt
        │
        ▼
rc review create
        │
        ▼
Review Markdown generated
```

This workflow runs end-to-end today. At this point Recall Commander is a
usable personal knowledge assessment tool.

Every future feature should extend this workflow rather than replace it.

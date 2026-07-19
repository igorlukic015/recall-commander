# Recall Commander - CLI

## Overview

The `rc` command line application (project `RecallCommander.Cli`, assembly
name `rc`) is the primary interface for the complete assessment workflow:

```
rc init
      |
      v
rc source add <path>          (repeat per source)
      |
      v
rc scan
      |
      v
rc assessment create
      |
      v
(user answers in any editor)
      |
      v
rc attempt validate <file>    (optional check)
      |
      v
rc review create <file>
```

Built on Spectre.Console.Cli with Microsoft dependency injection.
`Program.cs` is a single line; `CommandAppFactory` is the composition root
that registers the Application, Markdown, Infrastructure and AI modules,
maps commands, and installs the global error handler.

Run during development with:

```
dotnet run --project RecallCommander.Cli -- <command>
```

## Commands

### rc init

Initializes the Recall Commander workspace: creates the data directory and
the SQLite metadata database with its schema. Idempotent — running it again
reports "already initialized" and changes nothing.

```
$ rc init
Initialized Recall Commander workspace.
Database: /home/user/.local/share/RecallCommander/recall-commander.db
```

Every command that needs the database fails with
`Workspace is not initialized. Run 'rc init' first.` until this has run.

### rc source add \<path\>

Registers a directory as a question source. The path is normalized
(`~` expansion, made absolute, trailing separator trimmed) before storing.

Outcomes:

- `Registered question source: <path>` — added (exit code 0)
- `Question source is already registered: <path>` — no-op (exit code 0)
- `Directory not found: <path>` — rejected (exit code 1)

### rc source remove \<path\>

Unregisters a question source. Only the registration is removed — the
directory and its contents are never touched. The directory does not need
to exist anymore; removing a stale registration is the main use case.

Outcomes:

- `Removed question source: <path>` — removed (exit code 0)
- `Question source is not registered: <path>` — nothing matched, with a
  hint to check `rc source list` (exit code 1)

### rc source list

Lists registered sources as a table with Id, Path and Registered (UTC)
columns. With no sources, prints a hint to use `rc source add <path>`.

### rc scan

Scans every registered source recursively for `*.md` files, parses their
Question Blocks and reports the result. Nothing is persisted — questions
exist only in memory during the command.

```
$ rc scan
Scanning...

questions.md
  Found 3 questions

Warnings:

CSharp/boxing.md:42
Missing required field 'type'.

Scan completed.

Questions discovered: 3
```

Behavior:

- files reachable from overlapping sources are scanned once
- a missing source directory or unreadable file becomes a warning, never an
  abort
- invalid Question Blocks are skipped and reported with `file:line`
- exit code 0 (also with warnings); with no sources registered, prints a
  hint and exits 0

### rc assessment create [--count \<COUNT\>]

Scans all sources, selects questions at random (no duplicates) and writes a
new assessment artifact to `Assessments/` under the current working
directory.

Options:

- `--count <COUNT>` — number of questions to include, default 10; must be
  greater than zero. If fewer questions exist, all of them are used.

```
$ rc assessment create --count 5
Assessment created.

Questions: 5

Output:
Assessments/assessment-2026-07-19-001.md
```

With no questions discovered: `No questions found. Check your sources with
rc scan.` and exit code 1.

### rc assessment list

Lists the generated assessments in the `Assessments/` directory under the
current working directory, newest first (artifact names embed date and
sequence number, so ordinal descending file name order is newest first).
There is no database of assessments — the files on disk are the record;
this is the same listing the Workbench's Assessments panel shows.

```
$ rc assessment list
┌──────────────────────────────┬──────────────────────────────────────────┐
│ Assessment (newest first)    │ Path                                     │
├──────────────────────────────┼──────────────────────────────────────────┤
│ assessment-2026-07-19-001.md │ Assessments/assessment-2026-07-19-001.md │
└──────────────────────────────┴──────────────────────────────────────────┘
```

With no assessments, prints a hint to use `rc assessment create` and exits 0.

### rc attempt validate \<file\>

Parses a completed assessment (attempt) file and reports whether it can be
reviewed. Read-only: the file is never modified.

Valid attempt (exit code 0):

```
$ rc attempt validate Attempts/assessment-2026-07-19-001.attempt.md
Attempt is valid.

Title: Assessment 2026-07-19
Questions: 2
Answered: 2
```

Invalid attempt (exit code 1): `Attempt is not valid.` followed by every
diagnostic as `file:line` plus a message — parsing collects all problems in
one pass rather than stopping at the first.

See [ArtifactLifecycle.md](ArtifactLifecycle.md) for the exact document
structure the parser expects.

### rc review create \<file\>

Validates the attempt file, evaluates every question through the configured
evaluator, and writes a new review artifact to `Reviews/` under the current
working directory. The attempt file stays untouched.

```
$ rc review create Attempts/assessment-2026-07-19-001.attempt.md
Review created.

Questions: 2

Output:
Reviews/review-2026-07-19-001.md
```

Failure modes (exit code 1): file not found; invalid attempt (with the same
diagnostics as `rc attempt validate` and a hint to run it); `AiException`
when a configured AI provider is misconfigured or unreachable.

Which evaluator runs — the deterministic fake (default), Ollama or Gemini —
is configuration; see [AIArchitecture.md](AIArchitecture.md).

## Exit Codes

`0` on success (including benign no-ops like re-initializing or re-adding a
source), `1` on any failure. The global exception handler prints
user-facing errors (`WorkspaceNotInitializedException`, `AiException`,
usage errors) as a plain red message; unexpected exceptions get a shortened
stack trace.

## Where Things Are Stored

| What | Where |
|---|---|
| Metadata database | `<data dir>/recall-commander.db`; data dir is the per-user application data directory (Linux: `~/.local/share/RecallCommander`), overridable with the `RC_DATA_DIR` environment variable |
| Assessments | `Assessments/` under the current working directory |
| Reviews | `Reviews/` under the current working directory |
| Attempts | Wherever the user saves them — RC never chooses |

Artifacts are named `{slug}-{yyyy-MM-dd}-{NNN}.md` and existing files are
never overwritten.

## Configuration

Configuration sources, later ones winning:

1. `appsettings.json` next to the binary
2. .NET user secrets (id `recall-commander`)
3. a gitignored `.env` file in the current working directory —
   `KEY=VALUE` lines, `#` comments, optional quotes, optional `export `
   prefix, `__` as section separator (`Ai__Gemini__ApiKey` →
   `Ai:Gemini:ApiKey`); malformed lines are skipped, never fatal
   (implemented in `RecallCommander.Infrastructure`, shared with the
   Workbench)

The Workbench uses the same chain and the same user secrets id, so an AI
provider configured once works in both frontends.

The only configuration section today is `Ai` (see
[AIArchitecture.md](AIArchitecture.md)).

## Testability

`CommandAppFactory.Create()` accepts optional overrides used by the
end-to-end tests:

- `configureServices` — replace boundary services (data paths, artifact
  output location) after the standard registrations
- `console` — a Spectre test console to capture output
- `configuration` — pinned configuration instead of the
  appsettings/secrets/.env chain

The command pipeline under test is exactly what production runs.

## Planned Commands

Not implemented yet, mentioned in MVP planning:

- `rc stats`

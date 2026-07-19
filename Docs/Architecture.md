# Recall Commander - Architecture

## Overview

Recall Commander is a Markdown-first knowledge assessment engine.

The application provides an assessment layer over user-owned Markdown question sources.

The system is responsible for:

* discovering questions
* parsing question definitions
* generating assessments
* processing attempts
* generating reviews
* storing application metadata

The system is not responsible for storing user knowledge.

The complete artifact workflow this architecture implements is described in
[ArtifactLifecycle.md](ArtifactLifecycle.md):

```
Question Block
      |
      v
   Question
      |
      v
  Assessment
      |
      v
   Attempt
      |
      v
    Review
```

---

# Architectural Principles

## Separation of Concerns

Recall Commander is divided into separate layers:

* Domain layer
* Contracts layer
* Application layer
* Infrastructure layer (SQLite, filesystem)
* Markdown layer (parsing and rendering)
* AI layer (answer evaluation providers)
* Presentation layer (CLI and Workbench)

Each layer has a clear responsibility and is independently testable.

---

## Dependency Direction

Dependencies flow inward.

```
   Cli            Workbench
    |                 |
    +--------+--------+
             |
             v
        Application
             |
             v
         Contracts
             |
             v
           Domain


Infrastructure    Markdown    AI
      |               |        |
      v               v        v
   Contracts  <-------+--------+
```

The Domain layer has no dependencies on external systems.

Infrastructure, Markdown and AI implement the interfaces defined in
Contracts without depending on Application: they reference Contracts (and
Domain) directly, so the concrete workflows in Application stay free to
change without forcing a rebuild of the implementation projects.

The two presentation projects (Cli and Workbench) are composition roots: they
wire every module together through dependency injection and contain no
business logic.

---

# Solution Structure

All projects live directly in the repository root next to the solution file
(`RecallCommander.slnx`). There are no `src/` or `tests/` folders.

```
RecallCommander/
│
├── RecallCommander.Cli/                  # Spectre.Console command line app ("rc")
├── RecallCommander.Workbench/            # Avalonia desktop app (dual-pane UI)
│
├── RecallCommander.Application/          # Use-case services
├── RecallCommander.Contracts/            # Cross-boundary interfaces and models
├── RecallCommander.Domain/               # Core business concepts
│
├── RecallCommander.Infrastructure/       # SQLite, filesystem, artifact storage
├── RecallCommander.Markdown/             # Question Block parsing, artifact rendering
├── RecallCommander.AI/                   # AI evaluation providers (Ollama, Gemini)
│
├── RecallCommander.Domain.Tests/
├── RecallCommander.Application.Tests/
├── RecallCommander.Markdown.Tests/
├── RecallCommander.AI.Tests/
├── RecallCommander.Workbench.Tests/
├── RecallCommander.IntegrationTests/     # CLI end-to-end and integration tests
│
├── Docs/
├── RecallCommander.slnx
└── README.md
```

The target framework is .NET 10 (`net10.0`), set once in
`Directory.Build.props`. Package versions are managed centrally in
`Directory.Packages.props`.

---

# Project Responsibilities

## RecallCommander.Domain

The core business concepts. No dependency on filesystem, database, Markdown,
CLI or external services.

Current types:

```
Domain/
    Question.cs            # A parsed Question Block (type, prompt, reference answer, concepts)
    QuestionType.cs        # Recall | Explanation | Synthesis
    QuestionSource.cs      # A registered source directory
    Assessment.cs          # Assessment + AssessmentQuestion (prompt-only snapshot)
    Attempt.cs             # Attempt + AttemptQuestion (prompt + user answer)
    Review.cs              # Review + QuestionReview (prompt + answer + evaluation)
    ReviewEvaluation.cs    # Score 0-10, understanding level, feedback lists
    UnderstandingLevel.cs  # Poor | Weak | Partial | Good | Strong | Excellent
    DomainException.cs     # Thrown when a domain invariant is violated
```

Entities validate their invariants in their constructors (non-empty titles,
at least one question, score range, ...) and throw `DomainException`
otherwise.

---

## RecallCommander.Contracts

The abstractions shared across layer boundaries: interfaces implemented by
Infrastructure, Markdown and AI, and the models that travel through those
interfaces' signatures. Depends only on Domain.

A type belongs here when something other than Application needs it without
depending on Application itself. Result and status DTOs owned by a single
concrete Application service (nothing implements them elsewhere) stay in
Application instead.

By that rule Contracts also holds one small piece of shared behavior:
`ArtifactFileNameGenerator`, the artifact naming scheme
("assessment-2026-07-16-001.md") used by both Application (to build the
date-stamped stem) and Infrastructure (to probe sequenced candidate names
in the file store).

Current structure:

```
Contracts/
    Interfaces/
        Artifacts/
            IArtifactRenderer.cs        # Renders one artifact type to Markdown
            IArtifactStore.cs           # Persists rendered artifacts (never overwrites)
            IArtifactWriter.cs          # Render + persist pipeline, one per artifact type
            IArtifactOutputPathProvider.cs
        Assessments/
            IQuestionSelector.cs        # Chooses questions for an assessment
        Attempts/
            IAttemptParser.cs           # Attempt document -> Attempt
        FileSystem/
            IFileSystem.cs              # Path normalization, Markdown enumeration, reads
        Questions/
            IQuestionBlockParser.cs     # Markdown document -> questions + diagnostics
        Reviews/
            IQuestionEvaluator.cs       # Evaluates one question/answer pair
        Sources/
            IQuestionSourceRepository.cs
        Workspace/
            IWorkspaceInitializer.cs    # Creates the metadata store (idempotent)

    Artifacts/
        ArtifactFileNameGenerator.cs    # "assessment-2026-07-16-001.md" naming scheme,
                                        # shared by Application and Infrastructure

    Models/
        Artifacts/SavedArtifact.cs
        Attempts/AttemptParseResult.cs
        Parsing/ParseDiagnostic.cs      # Problem + 1-based line number
        Questions/QuestionParseResult.cs
        Workspace/InitializationResult.cs

    Exceptions/
        WorkspaceNotInitializedException.cs
```

---

## RecallCommander.Application

The use cases. Coordinates the domain and the Contracts interfaces; never
touches the filesystem, database or network directly.

Current structure:

```
Application/
    Sources/
        QuestionSourceService.cs      # Register and list question sources
        AddSourceResult.cs
    Scanning/
        ScanService.cs                # Scan all sources, aggregate questions + warnings
        ScanReport.cs
    Assessments/
        CreateAssessmentService.cs    # Scan -> select -> snapshot -> write artifact
        RandomQuestionSelector.cs     # IQuestionSelector: Fisher-Yates, no duplicates
        AssessmentLocator.cs          # Lists generated assessments from disk, newest first
                                      # (used by 'rc assessment list' and the Workbench)
    Attempts/
        ValidateAttemptService.cs     # Read + parse an attempt file (file stays untouched)
    Reviews/
        CreateReviewService.cs        # Validate attempt -> evaluate -> write review artifact
        FakeQuestionEvaluator.cs      # Deterministic IQuestionEvaluator stand-in
    Artifacts/
        ArtifactWriter.cs             # Generic name -> render -> persist pipeline
```

Services return status records (for example `CreateAssessmentResult`,
`ValidateAttemptResult`) instead of throwing for expected outcomes; the
presentation layers turn those into console or UI output.

`ArtifactWriter<T>` is registered as an open generic: `IArtifactWriter<T>`
resolves for any `T` that has a registered `IArtifactRenderer<T>`, so a new
artifact type only needs a renderer.

---

## RecallCommander.Markdown

All Markdown-specific functionality, built on Markdig and YamlDotNet.

```
Markdown/
    Parsing/
        RcContainerBlock.cs       # A ':::rc-*' fenced container
        RcContainerParser.cs      # Markdig block parser for nested ':::' fences
        QuestionBlockParser.cs    # IQuestionBlockParser: rc-question -> Question
        AttemptParser.cs          # IAttemptParser: completed assessment -> Attempt
    Writing/
        MarkdownArtifactBuilder.cs    # Frontmatter + blocks, consistent formatting
        YamlFrontmatterSerializer.cs  # snake_case keys, artifact timestamp format
        AssessmentRenderer.cs         # IArtifactRenderer<Assessment>
        ReviewRenderer.cs             # IArtifactRenderer<Review>
```

Parsing never stops at the first error: problems are collected as
`ParseDiagnostic` values (message + 1-based line number) so every problem in
a document is reported at once. Renderers never touch the filesystem;
persistence is the store's job.

---

## RecallCommander.Infrastructure

External systems: SQLite and the physical filesystem.

```
Infrastructure/
    Database/
        DataPaths.cs                      # Data directory + database path resolution
        SqliteConnectionFactory.cs        # Opens connections; guards workspace init
        WorkspaceInitializer.cs           # Creates schema; idempotent
        SqliteQuestionSourceRepository.cs # Dapper-based IQuestionSourceRepository
    FileSystem/
        PhysicalFileSystem.cs             # IFileSystem: ~ expansion, recursive *.md enumeration
    Artifacts/
        ArtifactFileStore.cs              # IArtifactStore: sequenced names, never overwrites
        CurrentDirectoryArtifactOutputPathProvider.cs
    Configuration/
        DotEnvConfigurationExtensions.cs  # .env configuration source (shared by CLI and Workbench)
```

Application data lives in the per-user application data directory
(for example `~/.local/share/RecallCommander/recall-commander.db` on Linux).
The `RC_DATA_DIR` environment variable overrides the location, which is also
how tests isolate their workspaces.

---

## RecallCommander.AI

AI answer evaluation behind the `IQuestionEvaluator` boundary. Described in
detail in [AIArchitecture.md](AIArchitecture.md).

```
AI/
    Clients/          # IAiClient + Ollama and Gemini implementations
    Configuration/    # AiOptions ("Ai" section), OllamaOptions, GeminiOptions
    Evaluation/       # AiQuestionEvaluator, prompt builder, JSON response parser
    Prompts/          # Embedded prompt Markdown (Review/SystemPrompt.md, UserPrompt.md)
    AiException.cs
```

The default provider is `fake` (the deterministic evaluator in Application);
nothing reaches the network unless a real provider is configured.

---

## RecallCommander.Cli

The `rc` command line application, built on Spectre.Console.Cli. Documented in
[CLI.md](CLI.md).

```
Cli/
    Program.cs               # return CommandAppFactory.Create().Run(args);
    CommandAppFactory.cs     # Composition root: DI, commands, configuration, error handling
    Commands/                # One class per command
    Infrastructure/
        TypeRegistrar.cs     # Bridges Microsoft DI into Spectre
    appsettings.json
```

The CLI contains no business logic; commands call Application services and
format their result records.

---

## RecallCommander.Workbench

The Avalonia desktop application, a dual-pane UI styled after Total
Commander. Documented in [Workbench.md](Workbench.md).

It uses exactly the same Application, Markdown, Infrastructure and AI
registrations as the CLI — including the same configuration chain and user
secrets id — plus UI-only services (dialogs, external file opening).

---

# Data Flow

## Question Discovery (`rc scan`)

```
SQLite (registered sources)
      |
      v
ScanService
      |
      v
IFileSystem.EnumerateMarkdownFiles     (recursive, stable order,
      |                                 overlapping sources deduplicated)
      v
QuestionBlockParser (per file)
      |
      v
ScanReport (questions + warnings)
```

Questions are never persisted. Every scan reparses the sources from scratch;
the Markdown files remain the authority.

## Assessment Creation (`rc assessment create`)

```
ScanService
      |
      v
IQuestionSelector (random, no duplicates)
      |
      v
Assessment (domain snapshot: prompts only)
      |
      v
IArtifactWriter<Assessment>
      |         (stem "assessment-YYYY-MM-DD" -> store assigns "-NNN" ->
      |          renderer embeds the id in frontmatter)
      v
Assessments/assessment-YYYY-MM-DD-NNN.md
```

## Attempt Validation (`rc attempt validate`)

```
Attempt file (user-authored Save As of an assessment)
      |
      v
ValidateAttemptService -> AttemptParser
      |
      v
Attempt or diagnostics (file is never modified)
```

## Review Generation (`rc review create`)

```
Attempt file
      |
      v
ValidateAttemptService
      |
      v
IQuestionEvaluator (fake | ollama | gemini) — one call per question
      |
      v
Review (domain snapshot: prompts, answers, evaluations)
      |
      v
IArtifactWriter<Review>
      |
      v
Reviews/review-YYYY-MM-DD-NNN.md
```

---

# Database Architecture

SQLite is used as application metadata storage. Access goes through Dapper.

Current schema (created by `WorkspaceInitializer`, schema version 1):

```sql
CREATE TABLE question_sources (
    id                INTEGER PRIMARY KEY AUTOINCREMENT,
    directory_path    TEXT    NOT NULL UNIQUE,
    registered_at_utc TEXT    NOT NULL
);

CREATE TABLE app_settings (
    key   TEXT PRIMARY KEY,
    value TEXT NOT NULL
);
```

The database is responsible for:

* registered question sources
* application settings (currently only `schema_version`)

The database is not responsible for:

* Markdown content
* question storage
* user answers
* assessment, attempt or review content

Every command that needs the database goes through
`SqliteConnectionFactory`, which throws `WorkspaceNotInitializedException`
when `rc init` has not been run.

---

# Dependency Injection

Each module contributes a `ServiceCollectionExtensions` method; the
presentation layers compose them:

```csharp
services.AddRecallCommanderApplication();
services.AddRecallCommanderMarkdown();
services.AddRecallCommanderInfrastructure();
services.AddRecallCommanderAi(configuration);
```

Notable registrations:

* `IArtifactWriter<>` → open generic `ArtifactWriter<>`
* `IQuestionEvaluator` → `FakeQuestionEvaluator` by default; the AI module
  registers `AiQuestionEvaluator` over it when a real provider is configured
* `TimeProvider` → `TimeProvider.System` (tests substitute a fake)

The CLI bridges this container into Spectre.Console through `TypeRegistrar`.

---

# Testing Strategy

Six test projects, all xUnit.

## Domain Tests

Entities, invariants, value normalization. No external dependencies.

## Application Tests

Workflow services with fakes for every Contracts interface (in-memory
filesystem, repository, store). Includes artifact naming and writer pipeline
behavior.

## Markdown Tests

Question Block parsing (valid, malformed, nested content), attempt parsing,
artifact rendering, frontmatter serialization.

## AI Tests

Prompt building, response parsing (fenced/noisy JSON, missing fields),
provider selection, client behavior against stubbed HTTP.

## Workbench Tests

ViewModel behavior against fake services.

## Integration Tests

* `Integration/` — real SQLite and filesystem behind the Contracts interfaces
* `EndToEnd/` — full CLI commands through `CommandAppFactory` with a test
  console, isolated data directory and temp working directories

---

# Future Architecture Extensions

## Scheduling Engine

A separate application service responsible for selecting questions over
time. `IQuestionSelector` is the seam: adaptive selection replaces
`RandomQuestionSelector` without touching assessment creation.

## Concept Graph

A future domain capability. Concepts are already captured per question as
metadata and can evolve into a graph structure.

## More Evaluators

`IQuestionEvaluator` is the seam for evaluation. Current implementations:
`FakeQuestionEvaluator` (deterministic), `AiQuestionEvaluator` (Ollama or
Gemini). Future: other providers, rule-based evaluation, evaluation that uses
reference answers and concepts.

---

# MVP Architecture Goal

The architecture supports one complete workflow:

```
Discover Questions
        ↓
Create Assessment
        ↓
Process Attempt
        ↓
Generate Review
```

This workflow is implemented end-to-end. The architecture should remain
simple until real requirements appear.

---

# Final Principle

Recall Commander is designed as a modular assessment engine.

The domain defines what the system means.

Contracts define the boundaries between layers.

The application defines what the system does.

Infrastructure, Markdown and AI define how external systems are accessed.

The CLI and the Workbench provide user interaction.

Each part should evolve independently.

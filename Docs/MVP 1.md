# Recall Commander - MVP 1

## Objective

The objective of MVP 1 is to build the smallest fully functional version of Recall Commander capable of assessing user knowledge from Markdown Question Blocks.

MVP 1 is complete when a user can:

1. Create Markdown documents containing Recall Commander Question Blocks.
2. Configure one or more Question Sources.
3. Scan Question Sources and discover questions.
4. Generate an Assessment Markdown file.
5. Answer questions by editing the Assessment copy.
6. Submit the completed Attempt Markdown file.
7. Generate a Review Markdown file.
8. Persist assessment metadata and history.

The goal is to validate the complete assessment workflow from beginning to end.

---

# Out of Scope

The following features are intentionally excluded from MVP 1:

* Adaptive scheduling
* Spaced repetition
* AI-generated questions
* Graphical user interface
* Obsidian plugin
* Community sharing
* Cloud synchronization
* Plugin system
* Concept dependency graph
* Knowledge visualization
* Multi-user support
* Authentication
* Mobile/Desktop applications
* Question identity tracking
* Question history synchronization

These features may or may not be introduced in future versions.

---

# Technology Stack

## Language

* C#
* .NET

## Database

* SQLite

Purpose:

SQLite stores metadata and application history.

SQLite is **not the source of truth for questions**.

Knowledge and assessment artifacts remain in Markdown files.

## Libraries

* Markdig
* YamlDotNet
* Spectre.Console
* Dapper or Entity Framework Core

(The final persistence choice can be made during implementation.)

## Testing

* xUnit

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

## Step 1

Initialize Recall Commander.

```bash
rc init
```

Creates:

```text
.rc/

    recall.db

    settings.json
```

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
Scanning sources...

CSharp.md
  Found 34 questions

Databases.md
  Found 21 questions

Warnings:
  CSharp.md line 420
  Invalid question block:
  Missing rc-prompt

Scan completed.

Questions discovered: 54
```

Questions are not stored in SQLite.

They exist as parsed objects during execution.

---

## Step 4

Generate an assessment.

```bash
rc assessment create
```

Produces:

```text
Assessments/

    assessment-2026-07-13-001.md
```

The assessment contains:

* title
* instructions
* metadata
* unanswered questions

Questions are copied into the assessment.

The assessment is independent from the original Question Sources.

---

## Step 5

Answer questions.

The user creates an Attempt by copying the Assessment file and filling answers.

Example:

```text
Assessments/

    assessment-2026-07-13-001.md

Attempts/

    assessment-2026-07-13-001.attempt.md
```

Recall Commander is not involved during editing.

The user may use:

* Obsidian
* VS Code
* Rider
* Vim
* any Markdown editor

---

## Step 6

Review the completed Attempt.

```bash
rc review assessment-2026-07-13-001.attempt.md
```

Recall Commander:

* parses answers
* evaluates responses
* generates review output

Example:

```text
Reviews/

    assessment-2026-07-13-001.review.md
```

---

# Core Modules

## Configuration

Responsibilities:

* application settings
* Question Sources
* configuration loading

---

## Markdown Scanner

Responsibilities:

* recursively scan Question Sources
* discover Markdown documents

---

## Question Block Parser

Responsibilities:

* parse Question Blocks
* validate syntax
* produce Question objects

---

## Assessment Generator

Responsibilities:

* select questions
* create Assessment Markdown files

Initial question selection may be random.

---

## Attempt Parser

Responsibilities:

* parse completed Assessment files
* extract user answers

---

## Review Generator

Responsibilities:

* create Review Markdown files
* attach feedback to answers

Evaluation implementation may initially be simple.

LLM evaluation is a future extension.

---

## Persistence

Responsibilities:

SQLite stores:

* Question Source configuration
* assessment metadata
* attempt metadata
* review metadata
* application configuration

SQLite does not store:

* questions
* answers
* knowledge content

---

# Core Domain Model

## Question

Represents a parsed Question Block.

Fields:

* Question Type
* Prompt
* Reference Answer (optional)
* Concepts

Questions are temporary parsed objects.

They do not have persistent IDs in MVP 1.

---

## Assessment

Represents a generated assessment artifact.

Contains:

* Creation Date
* Title
* Questions

Stored as Markdown.

---

## Attempt

Represents a completed assessment.

Contains:

* Assessment questions
* User answers

Stored as Markdown.

---

## Review

Represents evaluation of an Attempt.

Contains:

* Feedback
* Scores
* Missing information
* Incorrect statements
* Suggested Improvements

Stored as Markdown.

---

# CLI Commands

MVP should support:

```text
rc init

rc source add

rc source remove

rc source list

rc scan

rc assessment create

rc review
```

Potential future commands:

```text
rc stats

rc validate
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
rc review
        │
        ▼
Review Markdown generated
```

At this point Recall Commander is a usable personal knowledge assessment tool.

Every future feature should extend this workflow rather than replace it.

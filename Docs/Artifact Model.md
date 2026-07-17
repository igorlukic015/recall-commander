# Recall Commander Artifact Model

## Core Principle

Generated artifacts are Markdown-first learning documents, not database exports.

They should be:

- human readable
- editable with any Markdown editor
- portable
- understandable years later
- independent from Recall Commander internals

The database stores metadata only.

The Markdown files are the learning history.

## Artifact Lifecycle

The workflow is:

```
Question Sources
        |
        v
Question Blocks
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

Each stage creates a more complete historical artifact.

## Assessment

### Purpose

An Assessment is a generated snapshot of questions selected at a specific moment.

It answers:

> "What questions did Recall Commander ask me?"

### Important Properties

An Assessment:

- is independent from Question Sources
- contains copied question text
- does not depend on the original question files
- does not need question IDs
- should remain readable years later

If a user deletes or modifies the original question source, old assessments remain valid.

### Assessment File Format

Assessment uses YAML frontmatter.

Example:

```yaml
---
type: assessment
created: 2026-07-13T19:30:00
title: C# Internals Assessment
---
```

The body is normal Markdown.

Example:

```markdown
# C# Internals Assessment

Answer the questions below.

---

## Question 1

What is boxing in C#?

---

## Question 2

Explain how garbage collection works in .NET.
```

### What Assessment Contains

Contains:

- title
- instructions
- metadata
- questions

Does not contain:

- answers
- review information
- internal concepts
- source paths
- question IDs

### Question Metadata Visibility

Decision:

Question metadata should not be shown to the user.

Do not display:

```
Type: Explanation

Concepts:
- Garbage Collection
- Memory Management
```

The user should only see:

> Explain how garbage collection works in .NET.

### Internal Assessment Metadata

Question type can exist internally in frontmatter if needed.

Example:

```yaml
---
type: assessment
questions:
  - type: Recall
  - type: Explanation
  - type: Synthesis
---
```

But it is not part of the visible question content.

Concepts are not included in the assessment.

## Attempt

### Purpose

An Attempt represents the user's completed answers.

It answers:

> "What did I answer when I took this assessment?"

### User Workflow

The intended workflow:

1. Generate Assessment.

   Example: `assessment-001.md`

2. User chooses **Save As**.

   Creates: `attempt-001.md`

3. User fills answers.
4. User gives the file to RC for review.

### Important Properties

An Attempt:

- is independent
- contains the original questions
- contains user answers
- does not modify the Assessment
- remains a historical record

### Attempt Format

Attempt is almost identical to Assessment.

Difference: questions now have answers.

Example:

A Save As does not change the artifact type: the attempt keeps
`type: assessment`. A file is an attempt because the user hands it to the
attempt parser, not because it declares a different type.

```markdown
---
type: assessment
created: 2026-07-13T20:00:00
assessment: csharp-internals-assessment
---

# C# Internals Assessment

---

## Question 1

What is boxing in C#?

### Answer

Boxing is the conversion of a value type into an object reference type.

---

## Question 2

Explain how garbage collection works in .NET.

### Answer

Garbage collection automatically manages memory by...
```

### Answer Separator

Decision:

Use:

```
### Answer
```

Not:

```
:::rc-answer
```

Reason:

The Attempt is a human document, not a Question Source.

It does not need RC syntax.

## Review

### Purpose

A Review is the evaluation of an Attempt.

It answers:

> "How well did I understand the material?"

### Important Properties

A Review:

- contains the Attempt content
- preserves the user's answers
- adds evaluation after each answer
- is a learning artifact itself

The review should not be a separate summary only.

The full context matters.

### Review Format

Uses YAML frontmatter.

Example:

```yaml
---
type: assessment-review
created: 2026-07-13T21:00:00
---
```

Structure:

```markdown
# C# Internals Assessment Review

## Summary

Overall assessment of performance.

---

## Question 1

What is boxing in C#?

### Answer

User answer.

### Review

Evaluation of the answer.

Missing:
- detail A
- detail B

Score:
8/10
```

### Review Content

A review can contain:

- summary
- correctness evaluation
- missing concepts
- incorrect statements
- suggested improvements
- score

## Database Responsibility

SQLite does not store the actual artifacts.

It stores metadata.

Possible tables:

- QuestionSource
- Assessment
- Attempt
- Review
- Configuration

Example:

Assessment table:

- Id
- FilePath
- CreatedAt
- CompletedAt

Not:

- QuestionText
- Answer
- Concepts

## Question Identity Decision

Important decision:

No question IDs in MVP.

Reasons:

- questions live in user-controlled files
- files move
- lines change
- content changes
- sources appear/disappear

Trying to permanently track questions creates complexity.

### MVP Approach

Every scan:

```
Scan sources
        |
        v
Parse Question Blocks
        |
        v
Create current question collection
```

No synchronization.

No question history.

No identity.

## Source Philosophy

Question Sources are just inputs.

Examples:

- `~/ObsidianVault`
- `~/ProgrammingQuestions`
- `~/Research`

RC does not care about folder organization.

A valid source could contain `everything.md` with:

- biology questions
- programming questions
- history questions
- random trivia

The user decides organization.

## Future Considerations

These decisions leave room for:

- concept graphs
- scheduling
- adaptive testing
- LLM evaluation
- statistics
- community sharing

without forcing MVP complexity.

## Final Artifact Model

```
Question Sources
(User owned)

        |
        v

Question Blocks
(User authored)

        |
        v

Assessment
(RC generated snapshot)

        |
        v

Attempt
(User completed assessment)

        |
        v

Review
(RC evaluation artifact)
```

The core philosophy:

> Knowledge belongs to the user. Assessment belongs to Recall Commander. Learning history belongs to Markdown artifacts.

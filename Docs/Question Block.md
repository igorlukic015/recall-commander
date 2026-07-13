# Recall Commander - Question Block Specification

## Overview

A Recall Commander Question Block is a structured Markdown block used to define an assessment question.

Question Blocks are the only part of a user's Markdown files that Recall Commander reads.

Everything outside Question Blocks is ignored.

Question Blocks allow users to store questions inside any Markdown environment:

- personal notes
- Obsidian vaults
- dedicated question files
- research documents
- any other Markdown collection

Recall Commander does not own the source documents.

The user controls the organization and location of Question Blocks.

## Core Principles

### Markdown First

Question Blocks are designed to extend Markdown, not replace it.

A user should be able to open a Question Source file and read it normally without Recall Commander.

The syntax should remain:

- human-readable
- editable
- portable
- version controllable with Git

### Source Independence

Recall Commander does not require a specific folder structure.

Valid Question Sources include:

```
ProgrammingQuestions/
    csharp.md
    databases.md

ObsidianVault/
    Programming/
        CSharp/
            boxing.md
            generics.md

RandomFolder/
    everything.md
```

All of these are valid.

The organization of source files is the user's responsibility.

## Question Discovery

Recall Commander scans configured Question Sources recursively.

During scanning:

1. Markdown files are discovered.
2. Question Blocks are extracted.
3. Valid Question Blocks become Questions.
4. Invalid Question Blocks are skipped.
5. Parsing errors are reported.

The scan continues even if individual questions are malformed.

## Question Block Syntax

A Question Block uses nested Markdown blocks.

Basic structure:

```
:::

type: Recall

concepts:
- Concept A
- Concept B

:::

The question text goes here.

:::

:::

The optional reference answer goes here.

:::

:::
```

### Block Structure

A Question Block contains:

```
rc-question
|
├── Metadata
|
├── rc-prompt
|
└── rc-answer (optional)
```

### Metadata

Metadata uses YAML-style syntax.

Metadata must appear directly inside the rc-question block before nested content blocks.

Example:

```
:::

type: Explanation

concepts:
- Garbage Collection
- Memory Management

:::

Explain garbage collection.

:::

:::
```

### Question Type

The `type` field defines the category of the question.

MVP 1 supported values:

- `type: Recall`
- `type: Explanation`
- `type: Synthesis`

#### Recall

Purpose:

> Can I retrieve and explain a fact or idea?

Expected answer:

- short explanation
- usually one to three sentences
- demonstrates retrieval of knowledge

Example:

```
:::

type: Recall

:::

What is boxing in C#?

:::

:::
```

#### Explanation

Purpose:

> Can I explain a concept in detail?

Expected answer:

- longer explanation
- demonstrates understanding
- may include examples and reasoning

Example:

```
:::

type: Explanation

:::

Explain how garbage collection works in .NET.

:::

:::
```

#### Synthesis

Purpose:

> Can I connect multiple concepts and form a deeper understanding?

Expected answer:

- combines multiple areas of knowledge
- requires reasoning
- cannot be answered by recalling one isolated fact

Example:

```
:::

type: Synthesis

concepts:
- Garbage Collection
- Memory Allocation
- Performance Optimization

:::

How do memory allocation strategies affect application performance in managed languages?

:::

:::
```

### Concepts

The optional `concepts` field describes the concepts related to the question.

Example:

```yaml
concepts:
- Value Types
- Reference Types
- Boxing
```

Concepts are metadata.

They are not shown in generated assessments.

They exist to support future capabilities such as:

- statistics
- scheduling
- knowledge graphs
- analysis
- recommendations

A question may have:

- zero concepts
- one concept
- many concepts

Synthesis questions commonly contain multiple concepts.

### Prompt Block

The rc-prompt block contains the actual question.

Example:

```
:::

Explain the difference between value types and reference types in C#.

:::
```

The prompt:

- must exist
- contains Markdown
- can contain multiple paragraphs
- can contain lists
- can contain code examples

### Answer Block

The rc-answer block contains an optional reference answer.

Example:

```
:::

Boxing is the conversion of a value type into an object reference type.

The value is copied into an object allocated on the managed heap.

:::
```

The answer:

- is optional
- contains Markdown
- may contain multiple paragraphs
- may contain code examples
- may contain any valid Markdown

### Why Answers Are Optional

Not every question requires a predefined answer.

Examples:

#### Synthesis Questions

Some questions have many valid answers.

Example:

```
:::

type: Synthesis

concepts:
- Physics
- Engineering
- Economics

:::

How do technological limitations influence economic development?

:::

:::
```

The purpose is evaluating reasoning, not matching a predefined answer.

#### Future LLM Evaluation

Future versions may evaluate answers using:

- concepts
- reference answers
- previous reviews
- external evaluation models

A reference answer is helpful but not mandatory.

## Parsing Rules

### Everything Outside Blocks Is Ignored

Example:

```
# My C# Notes

Garbage collection is important.

:::

type: Recall

:::

What is garbage collection?

:::

:::

More notes.
```

Only the Question Block is parsed.

### Multiple Question Blocks Per File

A Markdown file may contain any number of Question Blocks.

Example:

```
:::

type: Recall

:::

Question one.

:::

:::

Some normal Markdown text.

:::

type: Explanation

:::

Question two.

:::

:::
```

### Block Order

Question Block order does not affect functionality.

Questions are independent.

The order in which questions are discovered may only be used for display purposes.

## Error Handling

Parser errors should not stop scanning.

Invalid questions are skipped.

Recall Commander reports:

- file name
- line number
- error description

Example:

```
Scan completed.

Warnings:

CSharp/boxing.md:42
Question skipped:
Missing required field 'type'

CSharp/generics.md:120
Question skipped:
Missing rc-prompt block
```

The user may fix the source and scan again.

## Required Fields

A valid Question Block requires:

### Type

Example:

```yaml
type: Recall
```

### Prompt

Example:

```
:::

Question text.

:::
```

## Optional Fields

### Concepts

Example:

```yaml
concepts:
- Concept A
- Concept B
```

### Reference Answer

Example:

```
:::

Reference answer.

:::
```

## MVP 1 Limitations

The following are intentionally not supported:

- Question IDs
- Question synchronization
- Permanent question tracking
- Question history
- Nested concepts
- Concept graphs
- Automatic question generation
- AI-created metadata

Questions are discovered from sources during scanning.

The source files remain the authority.

## Future Compatibility

Question Blocks are designed to evolve.

Future additions may include:

- difficulty
- tags
- references
- hints
- external links
- evaluation criteria
- custom metadata

New metadata should not change the meaning of existing Question Blocks.

The fundamental structure remains:

```
Question Block

    Metadata

    Prompt

    Optional Answer
```

## Design Philosophy

Recall Commander does not replace the user's Markdown workflow.

It adds an assessment layer on top.

The user owns:

- knowledge files
- organization
- question sources
- answers
- learning history

Recall Commander owns:

- assessment generation
- evaluation
- analysis

Knowledge belongs to the user.

Assessment belongs to Recall Commander.

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

A Question Block uses named `:::` fenced containers that nest.

Complete structure:

```
:::rc-question

type: Recall

concepts:
- Concept A
- Concept B

:::rc-prompt

The question text goes here.

:::

:::rc-answer

The optional reference answer goes here.

:::

:::
```

An opening fence carries a name (`:::rc-question`, `:::rc-prompt`,
`:::rc-answer`); a bare `:::` closes the innermost open block. In the
example above the first bare `:::` closes `rc-prompt`, the second closes
`rc-answer`, and the final one closes `rc-question`.

### Block Structure

A Question Block contains:

```
rc-question
|
├── Metadata          (YAML, directly inside rc-question, before nested blocks)
|
├── rc-prompt         (required)
|
└── rc-answer         (optional)
```

### Metadata

Metadata uses YAML syntax.

Metadata must appear directly inside the rc-question block before the nested content blocks.

Example:

```
:::rc-question

type: Explanation

concepts:
- Garbage Collection
- Memory Management

:::rc-prompt

Explain garbage collection.

:::

:::
```

### Question Type

The `type` field defines the category of the question. It is matched
case-insensitively.

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
:::rc-question

type: Recall

:::rc-prompt

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
:::rc-question

type: Explanation

:::rc-prompt

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
:::rc-question

type: Synthesis

concepts:
- Garbage Collection
- Memory Allocation
- Performance Optimization

:::rc-prompt

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

Blank entries are dropped, values are trimmed, and duplicates are removed.

Synthesis questions commonly contain multiple concepts.

### Prompt Block

The rc-prompt block contains the actual question.

Example:

```
:::rc-prompt

Explain the difference between value types and reference types in C#.

:::
```

The prompt:

- must exist and must not be empty
- contains Markdown
- can contain multiple paragraphs
- can contain lists
- can contain code examples

### Answer Block

The rc-answer block contains an optional reference answer.

Example:

```
:::rc-answer

Boxing is the conversion of a value type into an object reference type.

The value is copied into an object allocated on the managed heap.

:::
```

The answer:

- is optional (an empty rc-answer block is treated as absent)
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
:::rc-question

type: Synthesis

concepts:
- Physics
- Engineering
- Economics

:::rc-prompt

How do technological limitations influence economic development?

:::

:::
```

The purpose is evaluating reasoning, not matching a predefined answer.

#### LLM Evaluation

The AI evaluators currently judge the answer against the question alone
(see [AIArchitecture.md](AIArchitecture.md)). Future versions may also use:

- concepts
- reference answers
- previous reviews

A reference answer is helpful but not mandatory.

## Parsing Rules

Parsing is implemented as a custom Markdig block parser
(`RcContainerParser`) plus `QuestionBlockParser` in the
`RecallCommander.Markdown` project.

### Everything Outside Blocks Is Ignored

Example:

```
# My C# Notes

Garbage collection is important.

:::rc-question

type: Recall

:::rc-prompt

What is garbage collection?

:::

:::

More notes.
```

Only the Question Block is parsed.

### Multiple Question Blocks Per File

A Markdown file may contain any number of Question Blocks, interleaved with
normal Markdown.

### Fence Nesting

- An opening fence is `:::` followed by a name (e.g. `:::rc-question`).
- A bare `:::` always closes the **innermost** open block.
- Metadata is everything directly inside `rc-question` before the first
  nested block.

### Unknown Nested Blocks

Unknown `:::rc-*` containers nested inside an rc-question are ignored, so
future block types do not invalidate existing questions.

### Block Order

Question Block order does not affect functionality.

Questions are independent.

The order in which questions are discovered may only be used for display purposes.

## Error Handling

Parser errors never stop scanning.

Invalid questions are skipped, and every problem is reported as a warning with:

- file name (relative to its source directory)
- 1-based line number
- error description

Diagnostics produced by the current parser:

| Situation | Message |
|---|---|
| No `rc-prompt` inside an rc-question | `Missing rc-prompt block.` |
| Empty `rc-prompt` | `rc-prompt block is empty.` |
| Missing `type` metadata | `Missing required field 'type'.` |
| Unrecognized `type` value | `Unknown question type '...'. Supported types: Recall, Explanation, Synthesis.` |
| Metadata that is not valid YAML | `Invalid metadata: ...` |
| Two `rc-prompt` or two `rc-answer` blocks | `Duplicate rc-prompt block.` / `Duplicate rc-answer block.` |
| `rc-question` inside an `rc-question` | `Nested rc-question block. A previous block may be missing its closing ':::'.` |
| `rc-prompt`/`rc-answer` outside any `rc-question` | `':::rc-prompt' block found outside an rc-question block.` |

Example scan output:

```
Scan completed.

Warnings:

CSharp/boxing.md:42
Missing required field 'type'.

CSharp/generics.md:120
Missing rc-prompt block.
```

The user may fix the source and scan again.

## Required Fields

A valid Question Block requires:

### Type

```yaml
type: Recall
```

### Prompt

```
:::rc-prompt

Question text.

:::
```

## Optional Fields

### Concepts

```yaml
concepts:
- Concept A
- Concept B
```

### Reference Answer

```
:::rc-answer

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

New metadata should not change the meaning of existing Question Blocks
(unknown metadata fields are already ignored by the parser).

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

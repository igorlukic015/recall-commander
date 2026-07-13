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

---

# Architectural Principles

## Separation of Concerns

Recall Commander is divided into separate layers:

* Domain layer
* Application layer
* Infrastructure layer
* Presentation layer

Each layer has a clear responsibility and should remain independently testable.

---

## Dependency Direction

Dependencies flow inward.

```
Presentation
      |
      v
Application
      |
      v
Domain


Infrastructure
      |
      v
Application
```

The Domain layer has no dependencies on external systems.

---

# Solution Structure

```
RecallCommander/

│
├── src/
│
│   ├── RecallCommander.Cli/
│   │
│   ├── RecallCommander.Application/
│   │
│   ├── RecallCommander.Domain/
│   │
│   ├── RecallCommander.Infrastructure/
│   │
│   └── RecallCommander.Markdown/
│
├── tests/
│
│   ├── RecallCommander.Domain.Tests/
│   │
│   ├── RecallCommander.Application.Tests/
│   │
│   └── RecallCommander.Markdown.Tests/
│
├── docs/
│
└── README.md
```

---

# Project Responsibilities

## RecallCommander.Cli

The CLI project is the user interface.

Responsibilities:

* command parsing
* user input handling
* displaying results
* invoking application services

The CLI should contain minimal business logic.

---

## RecallCommander.Domain

The Domain project contains the core business concepts.

It should have no dependency on:

* filesystem
* database
* Markdown
* CLI
* external services

Responsibilities:

* define domain entities
* define domain rules
* represent business concepts

Example structure:

```
Domain/

    Question.cs

    QuestionType.cs

    Assessment.cs

    Attempt.cs

    Review.cs
```

---

## RecallCommander.Application

The Application project contains use cases.

It coordinates the domain and external services.

Responsibilities:

* execute workflows
* coordinate operations
* apply application rules

Example structure:

```
Application/

    Services/

        ScanSourceService.cs

        CreateAssessmentService.cs

        ReviewAttemptService.cs
```

The Application layer should depend on abstractions rather than concrete implementations.

---

## RecallCommander.Markdown

The Markdown project handles Markdown-specific functionality.

Responsibilities:

* parse Markdown documents
* extract structured data
* generate Markdown documents

Responsibilities include:

* Question Block parsing
* Assessment generation
* Attempt parsing
* Review generation

Example structure:

```
Markdown/

    Parsers/

        QuestionBlockParser.cs

        AttemptParser.cs


    Writers/

        AssessmentWriter.cs

        ReviewWriter.cs
```

---

## RecallCommander.Infrastructure

The Infrastructure project handles external systems.

Responsibilities:

* SQLite access
* filesystem access
* configuration loading
* external services

Example structure:

```
Infrastructure/

    Database/

        SqliteConnectionFactory.cs

        Repositories/


    FileSystem/

        FileScanner.cs


    Configuration/

        SettingsProvider.cs
```

---

# Data Flow

## Question Discovery

```
File System

      |

      v

Markdown Scanner

      |

      v

Markdown Parser

      |

      v

Domain Question Objects
```

---

## Assessment Creation

```
Question Collection

      |

      v

Assessment Service

      |

      v

Markdown Generator

      |

      v

Assessment File
```

---

## Review Generation

```
Attempt File

      |

      v

Attempt Parser

      |

      v

Review Service

      |

      v

Review Generator

      |

      v

Review File
```

---

# Database Architecture

SQLite is used as application metadata storage.

The database is responsible for:

* application configuration
* registered sources
* artifact metadata
* statistics

The database is not responsible for:

* Markdown content
* question storage
* user answers
* review content

---

# Dependency Injection

The application should use .NET dependency injection.

Example:

```
CLI

    ↓

Application Services

    ↓

Interfaces

    ↓

Infrastructure Implementations
```

Example:

```
IFileScanner

        |

        v

FileSystemScanner
```

```
IEvaluationEngine

        |

        v

EvaluationEngineImplementation
```

---

# Testing Strategy

## Domain Tests

Test:

* entities
* rules
* value objects

No external dependencies.

---

## Application Tests

Test:

* workflows
* service behavior
* orchestration

Use mocks or fakes for external dependencies.

---

## Markdown Tests

Test:

* parsing
* generation
* invalid input handling

Examples:

* valid question blocks
* malformed Markdown
* missing fields
* nested content

---

# Future Architecture Extensions

## Scheduling Engine

A separate application service responsible for selecting questions over time.

---

## Evaluation Engine

An abstraction over answer evaluation.

Possible implementations:

* rule-based evaluation
* local LLM evaluation
* cloud LLM evaluation

Example:

```
IEvaluationEngine

        |

        +-- LocalLlmEvaluationEngine

        +-- RuleBasedEvaluationEngine
```

---

## Concept Graph

A future domain capability.

The current architecture should allow concepts to evolve from simple metadata into a graph structure.

---

# MVP Architecture Goal

The architecture should support one complete workflow:

```
Discover Questions

        ↓

Create Assessment

        ↓

Process Attempt

        ↓

Generate Review
```

The architecture should remain simple until real requirements appear.

---

# Final Principle

Recall Commander is designed as a modular assessment engine.

The domain defines what the system means.

The application defines what the system does.

Infrastructure defines how external systems are accessed.

The CLI provides user interaction.

Each part should evolve independently.

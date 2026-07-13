# Recall Commander (RC)

## Project Vision

Recall Commander is a **Markdown-first knowledge assessment platform** designed to evaluate understanding rather than simply test memorization.

Unlike traditional flashcard or spaced repetition applications, Recall Commander does not own the user's knowledge. The knowledge remains in user-controlled Markdown files that can be edited in any text editor, version controlled with Git, and organized however the user prefers.

Recall Commander acts as an intelligent assessment layer on top of those files. It generates assessment sessions, evaluates answers, produces detailed review reports, and stores learning history and metadata separately from the knowledge itself.

The philosophy of the project is simple:

> **Knowledge belongs to the user. Assessment belongs to Recall Commander.**

---

# Core Philosophy

Recall Commander is intentionally designed around several principles.

## Ownership

Users own everything they create.

Knowledge files, assessment sessions, and review reports are stored in open, human-readable formats.

Recall Commander augments those files but never takes ownership of them.

The application should always enable users to access, edit, move, version, or replace their data without vendor lock-in.

---

## Markdown First

Knowledge is stored as Markdown files.

The application should never lock users into a proprietary database format.

The knowledge base should remain:

* human readable
* editable in any text editor
* version controllable using Git
* portable
* future proof

If Recall Commander disappeared tomorrow, every knowledge file would still be useful.

---

## The database Stores Metadata Only

The database is **not** the source of truth.

The database stores metadata that is generated during learning.

Examples include:

* sessions
* attempts
* scores
* statistics
* review history
* configuration
* future scheduling data

The actual knowledge always lives in Markdown.

---

## Assessment Over Memorization

Traditional flashcard applications answer:

> "Can you remember this?"

Recall Commander answers:

> "Do you truly understand this?"

The goal is to measure depth of understanding.

Questions therefore have multiple levels of complexity.

---

## Personal Knowledge Repository

Recall Commander does not provide a built-in knowledge base.

Instead, every user builds their own knowledge repository while learning, researching, reading books, watching courses, or studying any subject.

As users acquire new knowledge, they create Markdown knowledge files containing the concepts they want to retain and the assessment questions they wish to answer in the future.

The knowledge repository is therefore a personal, living collection of knowledge that grows over months and years.

It is expected to evolve alongside the user's learning journey.

Examples of topics may include:

* Programming
* Mathematics
* Physics
* Databases
* History
* Economics
* Philosophy
* Languages
* Engineering
* Any other field the user chooses to study

Recall Commander intentionally separates itself from traditional learning platforms that distribute predefined content.

The application provides **the assessment engine**, not the educational material.

Knowledge is created and owned by the user.

This philosophy has several advantages:

* Users retain complete ownership of their knowledge.
* Knowledge remains editable using any Markdown editor.
* The repository can be version controlled using Git.
* The repository is portable and independent of Recall Commander.
* Users are free to organize their knowledge however they wish.

Knowledge sharing is considered a future capability.

Users may choose to share parts or all of their repositories with others, allowing communities to exchange question banks and learning material.

However, this sharing is entirely optional.

Recall Commander itself will never ship with a predefined knowledge repository or curated educational content.

The application remains domain-agnostic.

Its responsibility is not to teach knowledge, but to assess the user's understanding of the knowledge they have chosen to learn.

---

# Goals

The primary goal is to become an intelligent knowledge assessment companion.

Eventually it should:

* generate assessment sessions
* evaluate answers
* identify weak concepts
* identify misconceptions
* recommend what to review
* track progress over months or years

Future versions may intelligently schedule reviews, but scheduling is **not part of MVP 1**.

---

# What Recall Commander Is NOT

Recall Commander is **not**:

* a note-taking application
* an Obsidian replacement
* a flashcard manager
* a spaced repetition application
* a document editor
* a knowledge generator
* a learning management system
* a content platform

Its responsibility is assessment.

---

# Guiding Principle

Recall Commander exists to answer one fundamental question:

> **"Do I truly understand what I have learned?"**

Every design decision should support that objective.

Knowledge belongs to the user. Assessment belongs to Recall Commander.

Everything else is secondary.

# Recall Commander - Workbench

## Overview

The Workbench (project `RecallCommander.Workbench`) is a cross-platform
desktop application built with Avalonia, styled as a dual-pane
Total Commander-like UI. It drives the same assessment workflow as the CLI
— every operation delegates to the same Application services the CLI
commands use; the Workbench only shapes their results into bindable state
and output messages.

```
dotnet run --project RecallCommander.Workbench
```

## Guiding Rule: Read-Only Artifacts

Markdown artifacts are user-owned documents. The Workbench:

- displays them **read-only** in the preview pane
- opens them in the user's own editor (OS default application) for editing
- **never writes to an artifact file**

Creating artifacts (assessments, reviews) goes through the same artifact
writers as the CLI, which only ever create new files.

## The Artifact Lifecycle in the Workbench

The UI walks the same lifecycle documented in
[ArtifactLifecycle.md](ArtifactLifecycle.md):

```
Question Block           (user's Markdown notes, edited outside the Workbench)
      |
      |  Add Source (F2) + Scan Sources (F3)
      v
Question                 (counts shown in the status bar)
      |
      |  Create Assessment (F4)  — question count box, default 10
      v
Assessment               (appears in the Assessments panel, newest first)
      |
      |  Open Externally -> Save As + answer in the user's own editor
      v
Attempt                  (Select Attempt (F5) picks the file,
                          Validate Attempt (F6) checks it)
      |
      |  Create Review (F7)
      v
Review                   (written to Reviews/, previewed read-only)
```

## Window Layout

```
+------------------------------------------------------------------+
| Menu: Sources | Assessment | Attempt | Review | Configuration |… |
| Toolbar: Add Source | Remove Source | Scan Sources |             |
|          Questions: [10] Create Assessment |                     |
|          Select Attempt | Validate Attempt | Create Review       |
+---------------------------+--------------------------------------+
| Question Sources          | Preview — <file> (read-only)         |
|   (registered paths)      |   (artifact text, monospace,         |
|                           |    read-only)                        |
+---------------------------+                                      |
| Assessments               +--------------------------------------+
|   (files, newest first)   | Output                               |
|                           |   (append-only operation log)        |
+---------------------------+--------------------------------------+
| Status: Sources | Files | Questions | Warnings    Attempt: <file>|
| F2 Add Source | F3 Scan | F4 Create | F5 Attempt | F6 Validate | |
| F7 Review | Alt+F4 Exit                                          |
+------------------------------------------------------------------+
```

- **Left column** — two stacked list panels: registered Question Sources
  and generated Assessments (listed from the `Assessments/` output
  directory, newest first by file name).
- **Right column** — the read-only Preview pane and the Output log, where
  every operation reports what the CLI would have printed.
- **Bottom** — a Total Commander-style function key bar and a status bar
  with the last scan summary and the selected attempt.
- **Configuration menu** — switches between light and dark theme variants.

## Features

| Action | Shortcut | What it does |
|---|---|---|
| Add Source | F2 | Folder picker → `QuestionSourceService.AddAsync` (same as `rc source add`) |
| Remove Source | — | Unregisters the selected source (same as `rc source remove`); asks for a selection if none; never touches the directory itself |
| Refresh Sources | — | Reloads the source list from the database |
| Scan Sources | F3 | `ScanService.ScanAsync`; warnings go to Output, totals to the status bar |
| Create Assessment | F4 | `CreateAssessmentService.CreateAsync` with the question count from the toolbar (default 10) |
| Refresh Assessments | — | Re-lists the `Assessments/` directory |
| Preview | — | Loads the selected assessment (or attempt) into the read-only preview |
| Open Externally | — | Opens the selected assessment with the OS default application; Output reminds the user to Save As to create an attempt |
| Select Attempt | F5 | File picker for the completed assessment file; also previews it |
| Validate Attempt | F6 | `ValidateAttemptService.Validate` (same as `rc attempt validate`); result and diagnostics go to Output |
| Create Review | F7 | `CreateReviewService.CreateAsync` (same as `rc review create`) on the selected attempt (asks for one if none is selected); reports the review path and previews the created review read-only |

On startup the Workbench initializes the workspace exactly like `rc init`
(idempotent), then loads the registered sources and existing assessments.

Failures never show stack traces: exceptions are turned into readable
Output blocks (`Could not scan. / Reason: ...`), with a friendly message
for the uninitialized-workspace case.

## Architecture

MVVM with CommunityToolkit.Mvvm:

```
Workbench/
    Program.cs                  # Avalonia bootstrap
    App.axaml.cs                # Composition root (DI container, main window)
    ViewModels/
        MainWindowViewModel.cs  # All commands and bindable state
        ViewModelBase.cs
    Views/
        MainWindow.axaml        # Dual-pane layout, themes, function keys
    Services/
        IDialogService.cs               # Folder/file pickers (abstraction)
        StorageProviderDialogService.cs # Avalonia storage provider implementation
        IExternalFileOpener.cs          # Open a file in the OS default app
        ShellFileOpener.cs              # Process.Start with UseShellExecute
```

The composition root registers exactly the same modules as the CLI —
`AddRecallCommanderApplication()`, `AddRecallCommanderMarkdown()`,
`AddRecallCommanderInfrastructure()`, `AddRecallCommanderAi(configuration)`
— plus the UI-only services above. The configuration chain is the same as
the CLI's (appsettings.json → user secrets id `recall-commander` → `.env`
in the working directory), so an AI provider configured for `rc` evaluates
Workbench reviews unchanged; with the default `fake` provider everything
stays offline.

There is no database of assessments: the Assessments panel is filled by
`AssessmentLocator` (from the Application project, shared with
`rc assessment list`), which simply lists the artifact output directory,
because the Markdown files are the source of truth. Artifact names embed
date and sequence number, so ordinal descending file name order is newest
first.

`RecallCommander.Workbench.Tests` exercises the view model against fake
dialog, opener and boundary services.

## Current Limitations

- Artifacts are listed from the current working directory's `Assessments/`
  folder, the same output location the CLI uses — start the Workbench from
  the directory where you keep your artifacts
- The preview is plain text, not rendered Markdown

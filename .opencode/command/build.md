---
description: Build the whole solution and fix any errors
---
Run `dotnet build` on the solution.

Note: `TreatWarningsAsErrors` and `EnforceCodeStyleInBuild` are on, so any
`.editorconfig` style warning (e.g. using `var`, `this.` qualification,
wrong namespace style) is a hard build error. If the build fails, fix the
cause and rebuild until it is clean. Report the final result.

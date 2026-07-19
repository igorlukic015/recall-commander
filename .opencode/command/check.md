---
description: Pre-commit gate — build then test, must be fully green
---
Run the full verification gate before a change is considered done:

1. `dotnet build` — must succeed with zero warnings (warnings are errors here).
2. `dotnet test` — every test project must pass.

Fix anything that is not green, then report the final build and test status.
Do not commit unless explicitly asked.

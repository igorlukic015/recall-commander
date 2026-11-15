---
description: Run the rc CLI. Pass CLI arguments after the command.
---
Run the Recall Commander CLI (`RecallCommander.Cli`, assembly `rc`) with these
arguments: `$ARGUMENTS`

Use:

```
dotnet run --project RecallCommander.Cli -- $ARGUMENTS
```

To keep the real workspace untouched, set `RC_DATA_DIR` to a fresh temp
directory first and run the commands there. The full end-to-end loop is:
`init` → `source add <dir>` → `scan` → `assessment create` → (answer a copy) →
`attempt validate <file>` → `review create <file>`.

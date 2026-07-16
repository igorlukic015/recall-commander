# Rules for Claude

Hard rules for working in this repository. These are also enforced as deny
permissions in `.claude/settings.json`.

- **Never use `--force`** (or `--force-with-lease`) with any command — no force
  pushes, force checkouts, or force flags of any kind.
- **Never run `rm -rf`** or any other recursive force delete.
- **Never run `git push`.** Committing is fine when asked; the user pushes
  manually.
- **Never go outside the project root folder.** Do not read, write, or modify
  anything outside this repository.

#!/usr/bin/env bash
#
# Simulates a user working through every current Recall Commander flow,
# with small pauses between commands to mimic real-world usage:
#
#   init → add a question source → re-add it (mistake) → list sources →
#   scan → create an assessment → answer it via Save As → validate the
#   attempt → validate a broken attempt (mistake)
#
# Everything runs in an isolated sandbox: RC_DATA_DIR and the working
# directory point into a fresh temp directory, so your real workspace and
# artifacts are never touched. The sandbox is left behind for inspection.
#
# Usage:
#   ./simulate-usage.sh            # ~3s pause between steps
#   PAUSE=0 ./simulate-usage.sh    # no pauses (e.g. CI smoke run)

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PAUSE="${PAUSE:-3}"

banner() { printf '\n\033[1m== %s ==\033[0m\n\n' "$1"; }

think() {
    # A short wait, as if the user is reading the output before typing.
    sleep "$PAUSE"
}

banner "Building the CLI once"
dotnet build "$REPO_ROOT/RecallCommander.Cli" --nologo --verbosity quiet

RC_BIN="$REPO_ROOT/RecallCommander.Cli/bin/Debug/net10.0/rc"
rc() {
    printf '\033[36m$ rc %s\033[0m\n' "$*"
    "$RC_BIN" "$@"
}

SANDBOX="$(mktemp -d /tmp/rc-user-simulation-XXXXXX)"
export RC_DATA_DIR="$SANDBOX/data"
QUESTIONS_DIR="$SANDBOX/notes"
WORK_DIR="$SANDBOX/work"
mkdir -p "$QUESTIONS_DIR" "$WORK_DIR"
cd "$WORK_DIR"

echo "Sandbox: $SANDBOX"

banner "The user initializes their workspace"
rc init
think

banner "The user writes some question notes"
cat > "$QUESTIONS_DIR/csharp.md" <<'EOF'
# C# fundamentals

:::rc-question

type: Recall

concepts:
- Boxing
- Value Types

:::rc-prompt

What is boxing in C#?

:::

:::rc-answer

Boxing converts a value type into an object on the heap.

:::

:::

:::rc-question

type: Synthesis

:::rc-prompt

How do allocation patterns affect application performance?

:::

:::
EOF

cat > "$QUESTIONS_DIR/dotnet.md" <<'EOF'
# .NET runtime

:::rc-question

type: Recall

:::rc-prompt

What is the CLR?

:::

:::rc-answer

The Common Language Runtime, the virtual machine that runs .NET code.

:::

:::
EOF

echo "Wrote $(ls "$QUESTIONS_DIR" | wc -l) note files to $QUESTIONS_DIR"
think

banner "The user registers the notes directory as a question source"
rc source add "$QUESTIONS_DIR"
think

banner "The user absent-mindedly adds the same directory again"
rc source add "$QUESTIONS_DIR"
think

banner "The user lists the registered sources"
rc source list
think

banner "The user scans for questions"
rc scan
think

banner "The user creates an assessment with 3 questions"
rc assessment create --count 3
think

ASSESSMENT_FILE="$(ls Assessments/assessment-*.md | head -n 1)"

banner "The user does Save As on the assessment and writes their answers"
# Simulates filling in every empty '### Answer' section of the saved copy.
awk '{ print } /^### Answer$/ { print ""; print "This is my answer, written from memory while reviewing my notes." }' \
    "$ASSESSMENT_FILE" > my-attempt.md
echo "Saved $ASSESSMENT_FILE as my-attempt.md and answered every question."
think

banner "The user validates their completed attempt"
rc attempt validate my-attempt.md
think

banner "The user accidentally validates a broken copy (deleted Answer headings)"
grep -v '^### Answer$' my-attempt.md > broken-attempt.md
rc attempt validate broken-attempt.md || true
think

banner "Done"
echo "All flows exercised. Sandbox left behind for inspection:"
echo "  workspace database: $RC_DATA_DIR"
echo "  question notes:     $QUESTIONS_DIR"
echo "  artifacts/attempts: $WORK_DIR"

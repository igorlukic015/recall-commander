# Recall Commander - AI Architecture

## Overview

The `RecallCommander.AI` project adds AI-based answer evaluation to the
review workflow. It sits behind a single boundary — `IQuestionEvaluator` in
Contracts — so nothing outside the project knows whether a review was
produced by a deterministic stub, a local model or a cloud model.

```
CreateReviewService
        |
        v
IQuestionEvaluator                 (Contracts)
        |
        +-- FakeQuestionEvaluator  (Application; default, deterministic)
        |
        +-- AiQuestionEvaluator    (AI project)
                  |
                  v
              IAiClient
                  |
                  +-- OllamaAiClient   (local, /api/chat)
                  |
                  +-- GeminiAiClient   (cloud, generateContent REST API)
```

The evaluator's `Name` (e.g. `fake`, `ollama/llama3.2`,
`gemini/gemini-2.0-flash`) is recorded in the `evaluator` frontmatter field
of every review artifact, so a review always says how it was produced.

## Design Principles

- **Offline by default.** The default provider is `fake`. With it, the AI
  module registers nothing and the deterministic `FakeQuestionEvaluator`
  from the Application project stays active — no command ever reaches the
  network unless a real provider is configured explicitly.
- **Provider is configuration, not code.** Which model evaluates answers is
  decided by the `Ai` configuration section at startup; callers only see
  `IQuestionEvaluator`.
- **Fail loudly, never silently.** A misconfigured provider or an unusable
  model response throws `AiException` with a user-safe message. A confused
  model can never produce a silently wrong review.
- **Secrets never live in committed files.** API keys come from user
  secrets or a gitignored `.env` file.

## Evaluation Flow

One evaluation call per question in the attempt:

```
prompt + answer
      |
      v
ReviewPromptBuilder          (loads prompt templates, fills {{question}}/{{answer}})
      |
      v
AiRequest                    (system prompt + user prompt, provider-independent)
      |
      v
IAiClient.CompleteAsync      (transport, serialization, authentication)
      |
      v
AiResponse                   (generated text + provider + model)
      |
      v
EvaluationResponseParser     (extracts and validates the JSON evaluation)
      |
      v
ReviewEvaluation             (domain object: score, level, feedback lists)
```

## Prompts

Prompts are Markdown files embedded in the AI assembly under `Prompts/`
(so the tool works from any working directory):

- `Prompts/Review/SystemPrompt.md` — instructs the model to act as a
  technical mentor, evaluate understanding rather than wording, and respond
  with a single JSON object of exactly this shape:

  ```json
  {
    "score": 0,
    "level": "Poor",
    "summary": "",
    "strengths": [],
    "missing_information": [],
    "incorrect_statements": [],
    "suggestions": []
  }
  ```

  An empty answer means the question was unanswered: score 0, level Poor.

- `Prompts/Review/UserPrompt.md` — a template with `{{question}}` and
  `{{answer}}` placeholders.

## Response Parsing

`EvaluationResponseParser` turns the model's reply into a
`ReviewEvaluation`:

- tolerates the noise models add around JSON (code fences, surrounding
  prose) by cutting the text between the first `{` and the last `}`
- accepts comments and trailing commas; property names are snake_case
- requires `score`, `level` and `summary`; `level` must parse to an
  `UnderstandingLevel`
- drops blank list entries, then relies on the `ReviewEvaluation` domain
  constructor for final validation (score 0–10, non-empty entries)
- anything that does not yield a valid evaluation throws `AiException`

## Providers

### fake (default)

Not part of the AI project at all: `FakeQuestionEvaluator` in Application
returns fixed feedback (8/10 Strong for answered questions, 0/10 Poor for
unanswered ones). It exists so the review workflow runs end-to-end without
external services, and it keeps CI and offline use deterministic.

### ollama

`OllamaAiClient` talks to a local Ollama server through `POST /api/chat`
(non-streaming), sending the system and user prompts as chat messages. No
authentication. Unreachable server errors include a hint to check
`ollama serve`.

Configuration (`Ai:Ollama`):

| Key | Default | Notes |
|---|---|---|
| `Endpoint` | `http://localhost:11434` | Base address |
| `Model` | — | Required, e.g. `llama3.2` |

### gemini

`GeminiAiClient` talks to the Gemini REST API
(`POST models/{model}:generateContent`), sending the system prompt as
`systemInstruction` and the API key in the `x-goog-api-key` header. 401/403
responses produce a clear "check your API key" error.

Configuration (`Ai:Gemini`):

| Key | Default | Notes |
|---|---|---|
| `Endpoint` | `https://generativelanguage.googleapis.com/v1beta` | Base address |
| `Model` | — | Required, e.g. `gemini-2.0-flash` |
| `ApiKey` | — | Required; user secrets or `.env` only |

## Configuration and Wiring

The CLI builds configuration from three sources, later ones winning:

1. `appsettings.json` next to the binary
2. .NET user secrets (id `recall-commander`), e.g.
   `dotnet user-secrets set Ai:Gemini:ApiKey <key>`
3. a gitignored `.env` file in the working directory, with `__` as the
   section separator:

   ```
   Ai__Provider=ollama
   Ai__Ollama__Model=llama3.2

   # or
   Ai__Provider=gemini
   Ai__Gemini__Model=gemini-2.0-flash
   Ai__Gemini__ApiKey=...
   ```

`AddRecallCommanderAi(configuration)` reads `Ai:Provider`
(case-insensitive; `fake` when absent) and wires the chosen provider:

- `fake` — registers nothing; the Application fake stays active
- `ollama` / `gemini` — registers the typed HTTP client and
  `AiQuestionEvaluator`, which wins over the fake registration
- anything else — registers an evaluator that throws
  `AiException("Unknown AI provider ...")` when resolved

Configuration mistakes surface when the evaluator is resolved, not at
startup — commands that don't use AI keep working with a broken AI
configuration.

## Error Handling

`AiException` is the single failure type for the AI boundary: provider
misconfigured, endpoint unreachable, non-success HTTP status, or unusable
response content. Messages are safe to show to the user, and the CLI's
exception handler prints them without a stack trace.

## Current Scope and Future Direction

Currently the AI layer only evaluates review answers, one question per
request, using the prompt text and the user's answer.

The boundary leaves room for:

- passing reference answers and concepts into the evaluation
- other providers behind `IAiClient`
- AI-assisted question generation (explicitly out of scope for MVP 1)

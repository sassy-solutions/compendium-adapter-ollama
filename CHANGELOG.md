# Changelog

All notable changes to `Compendium.Adapters.Ollama` will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Initial implementation of `IAIProvider` against the Ollama HTTP API
  (`http://localhost:11434` by default) using `Compendium.Abstractions.AI@1.0.1`.
- Chat completion via `POST /api/chat` — synchronous and streaming (newline-delimited
  JSON, not SSE).
- Embeddings via the newer `POST /api/embed` batch endpoint with automatic fallback
  to the legacy `POST /api/embeddings` single-prompt endpoint when the batched route
  isn't available.
- Tool calling on Ollama models that support it (llama3.2+, qwen2.5+, mistral-nemo+,
  command-r). Exposes `WithTools()` and `GetToolCalls()` extension methods.
- Optional model auto-pull (`OllamaOptions.AutoPullModel`) — on 404 / model-not-found
  the adapter calls `POST /api/pull` and retries the original request once. Disabled
  by default; recommended only for dev environments.
- Local-model discovery via `GET /api/tags` mapped to `IReadOnlyList<AIModel>` with
  best-effort capability detection (tools / vision / embedding).
- DI extensions `AddCompendiumOllama(IConfiguration)` and `AddCompendiumOllama(Action<OllamaOptions>)`.
- `samples/01-local-chat` — runnable sample that talks to a local Ollama and prints
  setup instructions when the server isn't reachable.
- Integration tests (`tests/Integration/Compendium.Adapters.Ollama.IntegrationTests/`)
  using `Testcontainers` + `ollama/ollama:latest`. Gated on `RUN_OLLAMA_INTEGRATION=1`
  so unit-only CI stays hermetic.

### Notes

- Ollama runs unauthenticated by default. Production deployments **must** put it
  behind a reverse proxy. See the README "Security" section.
- Streaming uses newline-delimited JSON; the adapter parses one `OllamaChatResponse`
  per line and yields the final chunk when `done=true`.
- Tool calling on Ollama is silently no-op on models that don't advertise the
  capability — verify your specific model + Ollama version before relying on it
  in production.

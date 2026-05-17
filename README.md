# `compendium-adapter-ollama`

[Ollama](https://ollama.com/) AI provider adapter for the [Compendium](https://github.com/sassy-solutions/compendium) event-sourcing framework. Implements `IAIProvider` from `Compendium.Abstractions.AI` against the Ollama HTTP API for **local LLM inference** — designed for developer workflows and **air-gapped production** environments.

Extracted from `sassy-solutions/compendium` per [ADR-0006](https://github.com/sassy-solutions/compendium/blob/main/docs/adr/0006-multi-repo-adapter-split.md) (multi-repo adapter split).

## Install

```bash
dotnet add package Compendium.Adapters.Ollama
```

```csharp
services.AddCompendiumOllama(builder.Configuration.GetSection("Ollama"));
```

## Quick-start

1. Install Ollama (one of):
   ```bash
   # macOS
   brew install ollama && brew services start ollama
   # Linux
   curl -fsSL https://ollama.com/install.sh | sh
   # Docker
   docker run -d -p 11434:11434 --name ollama ollama/ollama
   ```
2. Pull a model:
   ```bash
   ollama pull llama3.2          # general chat + tool calling
   ollama pull nomic-embed-text  # embeddings
   ```
3. Wire the adapter into your DI container:
   ```csharp
   services.AddCompendiumOllama(opt =>
   {
       opt.BaseUrl = "http://localhost:11434";
       opt.DefaultModel = "llama3.2";
       opt.DefaultEmbeddingModel = "nomic-embed-text";
   });
   ```
4. Use `IAIProvider` like any other Compendium AI provider:
   ```csharp
   var result = await provider.CompleteAsync(new CompletionRequest
   {
       Model = "llama3.2",
       Messages = new[] { Message.User("Tell me a joke.") },
   });
   ```

A runnable example lives in [`samples/01-local-chat`](samples/01-local-chat).

## Options

| Key                       | Default                    | Notes                                                                                       |
|---------------------------|----------------------------|---------------------------------------------------------------------------------------------|
| `BaseUrl`                 | `http://localhost:11434`   | Where the Ollama HTTP API listens.                                                          |
| `DefaultModel`            | `llama3.2`                 | Used when `CompletionRequest.Model` is empty.                                               |
| `DefaultEmbeddingModel`   | `nomic-embed-text`         | Used when `EmbeddingRequest.Model` is empty.                                                |
| `DefaultMaxTokens`        | `2048`                     | Maps to `options.num_predict`. Use `-1` to let the model finish naturally.                  |
| `TimeoutSeconds`          | `600`                      | Local inference can be slow on first load — default is intentionally generous.              |
| `AutoPullModel`           | `false`                    | On 404 / model-not-found, call `POST /api/pull` then retry. Dev-only — pulls can be huge.   |
| `EnableLogging`           | `false`                    | Logs full request/response bodies at `Debug`. Never enable in production with PII.          |
| `KeepAlive`               | `"5m"`                     | Forwarded as Ollama's `keep_alive` — how long the model stays resident in memory.           |

## Tool calling

Some Ollama models advertise OpenAI-compatible tool calling. Use the `WithTools()` extension:

```csharp
using Compendium.Adapters.Ollama.Tools;

var tools = new List<AgentTool>
{
    new("get_weather", "Returns the current weather.",
        """{"type":"object","properties":{"city":{"type":"string"}},"required":["city"]}"""),
};
var request = new CompletionRequest
{
    Model = "llama3.2",
    Messages = new[] { Message.User("What's the weather in Paris?") },
}.WithTools(tools);

var result = await provider.CompleteAsync(request);
var calls = result.Value.GetToolCalls(); // IReadOnlyList<AgentToolInvocation>
```

**Model support is uneven.** Tool calling generally works on:

| Model family            | Tool calling? |
|-------------------------|----------------|
| `llama3.2`, `llama3.3`  | yes            |
| `llama4` (when out)     | yes            |
| `qwen2.5`, `qwen3`      | yes            |
| `mistral-nemo`, `mixtral` | yes          |
| `command-r`, `command-r-plus` | yes      |
| older `llama2`, `llama3.1` | partial / no |
| embedding models        | no             |

Older models silently ignore the `tools` field; the response will simply have no `tool_calls`. Verify against your specific model + Ollama version before relying on tool flow in production.

## Streaming

Ollama streams **newline-delimited JSON** (not Server-Sent Events). The adapter parses chunks automatically — consume them as any other `IAsyncEnumerable<Result<CompletionChunk>>`:

```csharp
await foreach (var chunk in provider.StreamCompleteAsync(request, ct))
{
    if (chunk.IsFailure) break;
    Console.Write(chunk.Value.ContentDelta);
}
```

## Auto-pull

Set `opt.AutoPullModel = true` to automatically pull a missing model the first time it's requested. Useful for dev and one-shot ops; **not recommended for production** because pulls block the request for the duration of the download (often gigabytes).

## Security

Ollama runs **unauthenticated** by default on `localhost`. For production:

- **Never** expose the raw API to the public internet — there is no auth, no rate-limiting, no per-tenant isolation.
- Put it behind a reverse proxy that handles authentication (e.g. nginx + OIDC, or a Kubernetes ingress with mTLS).
- Bind Ollama to `127.0.0.1` only and let the proxy do the public-facing TLS termination.
- Treat the model surface as untrusted code execution context — anyone with API access can pull and run arbitrary models.

## Recommended models

| Use case                    | Suggested model                                  | Approx. size |
|------------------------------|--------------------------------------------------|--------------|
| General chat (development)   | `llama3.2`, `qwen2.5:7b`                         | 2–5 GB       |
| General chat (production)    | `llama3.3:70b`, `qwen2.5:72b`                    | 40–50 GB     |
| Tool calling (smallest)      | `llama3.2:3b`, `qwen2.5:3b`                      | 2–3 GB       |
| Code completion              | `qwen2.5-coder`, `deepseek-coder-v2`             | 4–8 GB       |
| Embeddings                   | `nomic-embed-text`, `mxbai-embed-large`          | 0.3–1 GB     |
| Tiny CI / smoke tests        | `qwen2.5:0.5b`, `tinyllama`, `all-minilm`        | 0.3–1 GB     |

## Versioning

Versions are driven by git tags via [MinVer](https://github.com/adamralph/minver). The first published version is `1.0.0-preview.0` (the orchestrator tags on merge — **do not tag from a feature branch**).

## Build & test locally

```bash
dotnet restore
dotnet build -c Release
dotnet test  -c Release --filter "FullyQualifiedName!~IntegrationTests"
```

Integration tests boot a real Ollama container and pull tiny models — opt in via:

```bash
RUN_OLLAMA_INTEGRATION=1 dotnet test -c Release --filter "FullyQualifiedName~IntegrationTests"
```

Expect them to take several minutes the first time the image and models are pulled.

## License

[MIT](LICENSE) — Copyright © 2026 Sassy Solutions.

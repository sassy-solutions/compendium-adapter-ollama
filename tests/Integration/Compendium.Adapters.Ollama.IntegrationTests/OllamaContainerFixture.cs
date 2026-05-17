// -----------------------------------------------------------------------
// <copyright file="OllamaContainerFixture.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Net.Http;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Compendium.Adapters.Ollama.IntegrationTests;

/// <summary>
/// xUnit fixture that boots a one-shot <c>ollama/ollama:latest</c> container, pulls a small
/// chat model and a small embedding model, and exposes the base URL to tests.
/// </summary>
/// <remarks>
/// Slow on first run (pulls ~2 GB of image + model). Marked <c>RequiresDocker</c> via the
/// <see cref="OllamaIntegrationTests"/> trait and gated on the <c>RUN_OLLAMA_INTEGRATION</c>
/// environment variable so it doesn't fire by default in unit-only CI.
/// </remarks>
public sealed class OllamaContainerFixture : IAsyncLifetime
{
    /// <summary>Test chat model — tiny enough to pull in CI within a few minutes.</summary>
    public const string ChatModel = "qwen2.5:0.5b";

    /// <summary>Test embedding model.</summary>
    public const string EmbeddingModel = "all-minilm";

    private IContainer? _container;

    /// <summary>Gets the base URL of the running Ollama container.</summary>
    public string BaseUrl => _container is null
        ? throw new InvalidOperationException("Container is not running.")
        : $"http://{_container.Hostname}:{_container.GetMappedPublicPort(11434)}";

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        _container = new ContainerBuilder("ollama/ollama:latest")
            .WithPortBinding(11434, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPath("/").ForPort(11434)))
            .Build();

        await _container.StartAsync();

        // Pull the tiny models once at startup so each test doesn't pay the cost.
        await PullModelAsync(ChatModel);
        await PullModelAsync(EmbeddingModel);
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    private async Task PullModelAsync(string model)
    {
        using var http = new HttpClient { BaseAddress = new Uri(BaseUrl + "/"), Timeout = TimeSpan.FromMinutes(15) };
        using var content = new StringContent(
            $"{{\"model\":\"{model}\",\"stream\":false}}",
            System.Text.Encoding.UTF8,
            "application/json");
        using var response = await http.PostAsync("api/pull", content);
        response.EnsureSuccessStatusCode();
    }
}

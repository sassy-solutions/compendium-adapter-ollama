// -----------------------------------------------------------------------
// <copyright file="TestFactories.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.Ollama.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Compendium.Adapters.Ollama.Tests.TestSupport;

/// <summary>
/// Test factories for building Ollama SUTs against a <see cref="MockHttpMessageHandler"/>.
/// </summary>
internal static class TestFactories
{
    public const string DefaultBaseUrl = "http://localhost:11434";

    public static OllamaOptions DefaultOptions(Action<OllamaOptions>? configure = null)
    {
        var options = new OllamaOptions
        {
            BaseUrl = DefaultBaseUrl,
            DefaultModel = "llama3.2",
            DefaultEmbeddingModel = "nomic-embed-text",
            DefaultMaxTokens = 1024,
            TimeoutSeconds = 60,
            AutoPullModel = false,
            EnableLogging = false,
            KeepAlive = "5m",
        };
        configure?.Invoke(options);
        return options;
    }

    public static (OllamaHttpClient Client, MockHttpMessageHandler Handler) CreateHttpClient(
        Action<OllamaOptions>? configure = null)
    {
        var handler = new MockHttpMessageHandler();
        var options = DefaultOptions(configure);
        var httpClient = new HttpClient(handler);
        var sut = new OllamaHttpClient(
            httpClient,
            Options.Create(options),
            NullLogger<OllamaHttpClient>.Instance);
        return (sut, handler);
    }

    public static OllamaAIProvider CreateProvider(
        OllamaHttpClient httpClient,
        Action<OllamaOptions>? configure = null)
    {
        var options = DefaultOptions(configure);
        return new OllamaAIProvider(
            httpClient,
            Options.Create(options),
            NullLogger<OllamaAIProvider>.Instance);
    }

    public static (OllamaAIProvider Provider, MockHttpMessageHandler Handler) CreateProviderWithHandler(
        Action<OllamaOptions>? configure = null)
    {
        var (client, handler) = CreateHttpClient(configure);
        var provider = CreateProvider(client, configure);
        return (provider, handler);
    }

    public static CompletionRequest SimpleCompletionRequest(string? model = null) =>
        new()
        {
            Model = model ?? "llama3.2",
            Messages = new List<Message> { Message.User("Hello") },
        };

    public static EmbeddingRequest SimpleEmbeddingRequest(int n = 1, string? model = null) =>
        new()
        {
            Model = model ?? "nomic-embed-text",
            Inputs = Enumerable.Range(0, n).Select(i => $"input-{i}").ToList(),
        };
}

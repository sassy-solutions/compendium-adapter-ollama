// -----------------------------------------------------------------------
// <copyright file="OllamaIntegrationTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Adapters.Ollama.IntegrationTests;

/// <summary>
/// End-to-end tests against a real Ollama container (via <see cref="OllamaContainerFixture"/>).
/// Set <c>RUN_OLLAMA_INTEGRATION=1</c> to run; otherwise they are skipped. Even when enabled,
/// they require a working Docker socket — the container image is ~2 GB and the model pulls
/// add bandwidth, so keep them opt-in.
/// </summary>
[Trait("Category", "RequiresDocker")]
[Collection("OllamaContainer")]
public sealed class OllamaIntegrationTests : IClassFixture<OllamaContainerFixture>
{
    private readonly OllamaContainerFixture _fixture;

    /// <summary>Initializes a new instance of the <see cref="OllamaIntegrationTests"/> class.</summary>
    public OllamaIntegrationTests(OllamaContainerFixture fixture)
    {
        _fixture = fixture;
    }

    private static bool Enabled =>
        string.Equals(Environment.GetEnvironmentVariable("RUN_OLLAMA_INTEGRATION"), "1", StringComparison.Ordinal);

    private IAIProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCompendiumOllama(opt =>
        {
            opt.BaseUrl = _fixture.BaseUrl;
            opt.DefaultModel = OllamaContainerFixture.ChatModel;
            opt.DefaultEmbeddingModel = OllamaContainerFixture.EmbeddingModel;
            opt.TimeoutSeconds = 600;
        });
        return services.BuildServiceProvider().GetRequiredService<IAIProvider>();
    }

    [SkippableFact]
    public async Task Chat_BasicRoundTrip_ReturnsAssistantContent()
    {
        Skip.IfNot(Enabled, "RUN_OLLAMA_INTEGRATION not set");

        var provider = BuildProvider();
        var result = await provider.CompleteAsync(new CompletionRequest
        {
            Model = OllamaContainerFixture.ChatModel,
            SystemPrompt = "Reply with the single word 'pong'.",
            Messages = new List<Message> { Message.User("ping") },
            MaxTokens = 16,
        });

        result.IsSuccess.Should().BeTrue(
            $"completion should succeed; error: {(result.IsFailure ? result.Error.Message : string.Empty)}");
        result.Value.Content.Should().NotBeNullOrWhiteSpace();
    }

    [SkippableFact]
    public async Task Embed_BasicRoundTrip_ReturnsVectorPerInput()
    {
        Skip.IfNot(Enabled, "RUN_OLLAMA_INTEGRATION not set");

        var provider = BuildProvider();
        var result = await provider.EmbedAsync(new EmbeddingRequest
        {
            Model = OllamaContainerFixture.EmbeddingModel,
            Inputs = new List<string> { "hello", "world" },
        });

        result.IsSuccess.Should().BeTrue(
            $"embedding should succeed; error: {(result.IsFailure ? result.Error.Message : string.Empty)}");
        result.Value.Embeddings.Should().HaveCount(2);
        result.Value.Embeddings[0].Vector.Length.Should().BeGreaterThan(0);
    }

    [SkippableFact]
    public async Task ListModels_ReportsAtLeastThePulledModels()
    {
        Skip.IfNot(Enabled, "RUN_OLLAMA_INTEGRATION not set");

        var provider = BuildProvider();
        var result = await provider.ListModelsAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Select(m => m.Id).Should().Contain(name =>
            name.StartsWith(OllamaContainerFixture.ChatModel, StringComparison.Ordinal));
    }

    [SkippableFact]
    public async Task HealthCheck_AgainstRunningServer_Succeeds()
    {
        Skip.IfNot(Enabled, "RUN_OLLAMA_INTEGRATION not set");

        var provider = BuildProvider();
        var result = await provider.HealthCheckAsync();

        result.IsSuccess.Should().BeTrue();
    }
}

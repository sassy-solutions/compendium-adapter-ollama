// -----------------------------------------------------------------------
// <copyright file="OllamaHttpClientTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.Ollama.Http;
using Compendium.Adapters.Ollama.Http.Models;
using Compendium.Adapters.Ollama.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace Compendium.Adapters.Ollama.Tests.Http;

/// <summary>
/// Unit tests for <see cref="OllamaHttpClient"/> — focuses on protocol-level edge cases.
/// </summary>
public class OllamaHttpClientTests
{
    [Fact]
    public async Task ChatAsync_OnTimeout_ReturnsTimeoutError()
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient();
        handler.When(HttpMethod.Post, "*/api/chat")
            .Throw(new TaskCanceledException("timed out"));

        // Act
        var result = await sut.ChatAsync(
            new OllamaChatRequest { Model = "llama3.2", Messages = new() },
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AI.Timeout");
    }

    [Fact]
    public async Task ChatAsync_OnHttpRequestException_ReturnsProviderUnavailable()
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient();
        handler.When(HttpMethod.Post, "*/api/chat")
            .Throw(new HttpRequestException("connection refused"));

        // Act
        var result = await sut.ChatAsync(
            new OllamaChatRequest { Model = "llama3.2", Messages = new() },
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AI.ProviderUnavailable");
    }

    [Fact]
    public async Task ChatAsync_WhenServerReturnsEmptyBody_ReturnsProviderError()
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient();
        handler.When(HttpMethod.Post, "*/api/chat").Respond("application/json", "null");

        // Act
        var result = await sut.ChatAsync(
            new OllamaChatRequest { Model = "llama3.2", Messages = new() },
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AI.ProviderError");
    }

    [Fact]
    public async Task ChatAsync_WhenServerReturnsInvalidJson_ReturnsProviderError()
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient();
        handler.When(HttpMethod.Post, "*/api/chat").Respond("application/json", "this is not json");

        // Act
        var result = await sut.ChatAsync(
            new OllamaChatRequest { Model = "llama3.2", Messages = new() },
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AI.ProviderError");
        result.Error.Message.Should().Contain("Invalid response");
    }

    [Fact]
    public async Task ChatStreamAsync_WithMalformedLine_SkipsItAndContinues()
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient();
        var stream = string.Join("\n",
            "{this is not json",
            "{\"model\":\"llama3.2\",\"message\":{\"role\":\"assistant\",\"content\":\"ok\"},\"done\":false}",
            "",
            "{\"model\":\"llama3.2\",\"message\":{\"role\":\"assistant\",\"content\":\"\"},\"done\":true}",
            "");
        handler.When(HttpMethod.Post, "*/api/chat").Respond("application/x-ndjson", stream);

        // Act
        var chunks = new List<OllamaChatResponse>();
        await foreach (var r in sut.ChatStreamAsync(
            new OllamaChatRequest { Model = "llama3.2", Messages = new() },
            CancellationToken.None))
        {
            r.IsSuccess.Should().BeTrue();
            chunks.Add(r.Value);
        }

        // Assert — first chunk was malformed and skipped; we should get 2 valid chunks
        chunks.Should().HaveCount(2);
        chunks[0].Message!.Content.Should().Be("ok");
        chunks[1].Done.Should().BeTrue();
    }

    [Fact]
    public async Task ChatStreamAsync_OnHttpError_YieldsErrorAndStops()
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient();
        handler.When(HttpMethod.Post, "*/api/chat")
            .Respond(HttpStatusCode.NotFound, "application/json",
                "{\"error\":\"model 'foo' not found, try pulling it first\"}");

        // Act
        var results = new List<Result<OllamaChatResponse>>();
        await foreach (var r in sut.ChatStreamAsync(
            new OllamaChatRequest { Model = "foo", Messages = new() },
            CancellationToken.None))
        {
            results.Add(r);
        }

        // Assert
        results.Should().ContainSingle();
        results[0].IsFailure.Should().BeTrue();
        results[0].Error.Code.Should().Be("AI.ModelNotFound");
    }

    [Fact]
    public async Task EmbedAsync_OnTimeout_ReturnsTimeoutError()
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient();
        handler.When(HttpMethod.Post, "*/api/embed")
            .Throw(new TaskCanceledException("slow"));

        // Act
        var result = await sut.EmbedAsync(
            new OllamaEmbedRequest { Model = "x", Input = new() { "y" } },
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AI.Timeout");
    }

    [Fact]
    public async Task EmbedAsync_OnHttpRequestException_ReturnsProviderUnavailable()
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient();
        handler.When(HttpMethod.Post, "*/api/embed")
            .Throw(new HttpRequestException("no route"));

        // Act
        var result = await sut.EmbedAsync(
            new OllamaEmbedRequest { Model = "x", Input = new() { "y" } },
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AI.ProviderUnavailable");
    }

    [Fact]
    public async Task EmbedLegacyAsync_OnTimeout_ReturnsTimeoutError()
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient();
        handler.When(HttpMethod.Post, "*/api/embeddings")
            .Throw(new TaskCanceledException("slow"));

        // Act
        var result = await sut.EmbedLegacyAsync(
            new OllamaLegacyEmbeddingsRequest { Model = "x", Prompt = "y" },
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AI.Timeout");
    }

    [Fact]
    public async Task EmbedLegacyAsync_OnHttpRequestException_ReturnsProviderUnavailable()
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient();
        handler.When(HttpMethod.Post, "*/api/embeddings")
            .Throw(new HttpRequestException("connection refused"));

        // Act
        var result = await sut.EmbedLegacyAsync(
            new OllamaLegacyEmbeddingsRequest { Model = "x", Prompt = "y" },
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AI.ProviderUnavailable");
    }

    [Fact]
    public async Task PullModelAsync_OnSuccess_ReturnsSuccessWhenLastStatusIsSuccess()
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient();
        handler.When(HttpMethod.Post, "*/api/pull")
            .Respond("application/x-ndjson", string.Join('\n',
                "{\"status\":\"pulling manifest\"}",
                "{\"status\":\"downloading\",\"completed\":1024,\"total\":2048,\"digest\":\"sha256:x\"}",
                "{\"status\":\"success\"}",
                ""));

        // Act
        var result = await sut.PullModelAsync("llama3.2", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task PullModelAsync_WhenStreamReportsError_ReturnsModelNotFound()
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient();
        handler.When(HttpMethod.Post, "*/api/pull")
            .Respond("application/x-ndjson", "{\"error\":\"pull manifest 401\"}\n");

        // Act
        var result = await sut.PullModelAsync("foo", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AI.ModelNotFound");
    }

    [Fact]
    public async Task PullModelAsync_WhenStreamEndsWithoutSuccess_ReturnsProviderError()
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient();
        handler.When(HttpMethod.Post, "*/api/pull")
            .Respond("application/x-ndjson", "{\"status\":\"pulling manifest\"}\n");

        // Act
        var result = await sut.PullModelAsync("foo", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AI.ProviderError");
    }

    [Fact]
    public async Task PullModelAsync_WithMalformedLine_SkipsAndContinues()
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient();
        handler.When(HttpMethod.Post, "*/api/pull")
            .Respond("application/x-ndjson",
                "garbage line\n{\"status\":\"success\"}\n");

        // Act
        var result = await sut.PullModelAsync("foo", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task PullModelAsync_OnHttpErrorBeforeStream_ReturnsProviderError()
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient();
        handler.When(HttpMethod.Post, "*/api/pull")
            .Respond(HttpStatusCode.InternalServerError, "application/json", "{\"error\":\"oops\"}");

        // Act
        var result = await sut.PullModelAsync("foo", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AI.ProviderError");
    }

    [Fact]
    public async Task PullModelAsync_OnTimeout_ReturnsTimeoutError()
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient();
        handler.When(HttpMethod.Post, "*/api/pull")
            .Throw(new TaskCanceledException("slow"));

        // Act
        var result = await sut.PullModelAsync("foo", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AI.Timeout");
    }

    [Fact]
    public async Task PullModelAsync_OnHttpRequestException_ReturnsProviderUnavailable()
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient();
        handler.When(HttpMethod.Post, "*/api/pull")
            .Throw(new HttpRequestException("connect failed"));

        // Act
        var result = await sut.PullModelAsync("foo", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AI.ProviderUnavailable");
    }

    [Fact]
    public async Task ListModelsAsync_OnUnexpectedException_ReturnsProviderUnavailable()
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient();
        handler.When(HttpMethod.Get, "*/api/tags")
            .Throw(new InvalidOperationException("boom"));

        // Act
        var result = await sut.ListModelsAsync(CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AI.ProviderUnavailable");
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "{}", "AI.InvalidApiKey")]
    [InlineData(HttpStatusCode.TooManyRequests, "{}", "AI.RateLimitExceeded")]
    [InlineData(HttpStatusCode.NotFound, "{\"error\":\"model 'x' not found, try pulling it first\"}", "AI.ModelNotFound")]
    [InlineData(HttpStatusCode.BadRequest, "{\"error\":\"model 'x' not found\"}", "AI.ModelNotFound")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "{}", "AI.ProviderUnavailable")]
    [InlineData(HttpStatusCode.GatewayTimeout, "{}", "AI.ProviderUnavailable")]
    [InlineData(HttpStatusCode.InternalServerError, "{\"error\":\"boom\"}", "AI.ProviderError")]
    [InlineData(HttpStatusCode.InternalServerError, "not json", "AI.ProviderError")]
    [InlineData(HttpStatusCode.InternalServerError, "", "AI.ProviderError")]
    public void ParseErrorBody_MapsStatusCodesToExpectedErrors(
        HttpStatusCode status,
        string body,
        string expectedCode)
    {
        // Act
        var error = OllamaHttpClient.ParseErrorBody(status, body);

        // Assert
        error.Code.Should().Be(expectedCode);
    }

    [Fact]
    public void Constructor_WhenBaseAddressAlreadySet_DoesNotOverwriteIt()
    {
        // Arrange — caller may have set a custom base address before handing the client over.
        var existing = new Uri("http://custom.local:11434/");
        var httpClient = new HttpClient(new MockHttpMessageHandler())
        {
            BaseAddress = existing,
        };

        // Act
        _ = new OllamaHttpClient(
            httpClient,
            Options.Create(TestFactories.DefaultOptions()),
            NullLogger<OllamaHttpClient>.Instance);

        // Assert
        httpClient.BaseAddress.Should().Be(existing);
    }
}

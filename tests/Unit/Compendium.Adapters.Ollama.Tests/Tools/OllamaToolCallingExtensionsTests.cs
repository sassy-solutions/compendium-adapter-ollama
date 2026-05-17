// -----------------------------------------------------------------------
// <copyright file="OllamaToolCallingExtensionsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.Ollama.Tools;

namespace Compendium.Adapters.Ollama.Tests.Tools;

/// <summary>
/// Unit tests for <see cref="OllamaToolCallingExtensions"/>.
/// </summary>
public class OllamaToolCallingExtensionsTests
{
    [Fact]
    public void WithTools_AttachesToolsUnderKnownKey()
    {
        // Arrange
        var request = new CompletionRequest
        {
            Model = "llama3.2",
            Messages = new List<Message> { Message.User("hi") },
        };
        var tools = new List<AgentTool>
        {
            new("name", "desc", null),
        };

        // Act
        var updated = request.WithTools(tools);

        // Assert
        updated.AdditionalParameters.Should().NotBeNull();
        updated.AdditionalParameters!.Should().ContainKey(OllamaToolCallingExtensions.ToolsKey);
        updated.AdditionalParameters[OllamaToolCallingExtensions.ToolsKey].Should().BeSameAs(tools);
    }

    [Fact]
    public void WithTools_PreservesExistingAdditionalParameters()
    {
        // Arrange
        var existing = new Dictionary<string, object> { ["foo"] = "bar" };
        var request = new CompletionRequest
        {
            Model = "x",
            Messages = new List<Message>(),
            AdditionalParameters = existing,
        };

        // Act
        var updated = request.WithTools(new List<AgentTool> { new("t", "d", null) });

        // Assert
        updated.AdditionalParameters!.Should().ContainKey("foo");
        updated.AdditionalParameters["foo"].Should().Be("bar");
    }

    [Fact]
    public void WithTools_WithNullRequest_Throws()
    {
        // Arrange
        CompletionRequest? request = null;

        // Act
        var act = () => request!.WithTools(new List<AgentTool>());

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithTools_WithNullTools_Throws()
    {
        // Arrange
        var request = new CompletionRequest { Model = "x", Messages = new List<Message>() };

        // Act
        var act = () => request.WithTools(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetToolCalls_WhenNoMetadata_ReturnsEmpty()
    {
        // Arrange
        var response = new CompletionResponse
        {
            Id = "x",
            Model = "y",
            Content = "",
            FinishReason = FinishReason.Stop,
            Usage = new UsageStats { PromptTokens = 0, CompletionTokens = 0 },
            CreatedAt = DateTime.UtcNow,
        };

        // Act
        var calls = response.GetToolCalls();

        // Assert
        calls.Should().BeEmpty();
    }

    [Fact]
    public void GetToolCalls_WhenMetadataKeyMissing_ReturnsEmpty()
    {
        // Arrange
        var response = new CompletionResponse
        {
            Id = "x",
            Model = "y",
            Content = "",
            FinishReason = FinishReason.Stop,
            Usage = new UsageStats { PromptTokens = 0, CompletionTokens = 0 },
            CreatedAt = DateTime.UtcNow,
            Metadata = new Dictionary<string, object> { ["other"] = "value" },
        };

        // Act
        var calls = response.GetToolCalls();

        // Assert
        calls.Should().BeEmpty();
    }

    [Fact]
    public void GetToolCalls_WhenMetadataValueWrongType_ReturnsEmpty()
    {
        // Arrange
        var response = new CompletionResponse
        {
            Id = "x",
            Model = "y",
            Content = "",
            FinishReason = FinishReason.Stop,
            Usage = new UsageStats { PromptTokens = 0, CompletionTokens = 0 },
            CreatedAt = DateTime.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                [OllamaToolCallingExtensions.ToolCallsMetadataKey] = "not a list",
            },
        };

        // Act
        var calls = response.GetToolCalls();

        // Assert
        calls.Should().BeEmpty();
    }

    [Fact]
    public void GetToolCalls_WhenMetadataHasInvocations_ReturnsThem()
    {
        // Arrange
        var invocations = new List<AgentToolInvocation>
        {
            new("get_weather", "{\"city\":\"Paris\"}", string.Empty, false, TimeSpan.Zero),
        };
        var response = new CompletionResponse
        {
            Id = "x",
            Model = "y",
            Content = "",
            FinishReason = FinishReason.ToolCall,
            Usage = new UsageStats { PromptTokens = 0, CompletionTokens = 0 },
            CreatedAt = DateTime.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                [OllamaToolCallingExtensions.ToolCallsMetadataKey] = (IReadOnlyList<AgentToolInvocation>)invocations,
            },
        };

        // Act
        var calls = response.GetToolCalls();

        // Assert
        calls.Should().HaveCount(1);
        calls[0].ToolName.Should().Be("get_weather");
    }

    [Fact]
    public void GetToolCalls_WithNullResponse_Throws()
    {
        // Arrange
        CompletionResponse? response = null;

        // Act
        var act = () => response!.GetToolCalls();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}

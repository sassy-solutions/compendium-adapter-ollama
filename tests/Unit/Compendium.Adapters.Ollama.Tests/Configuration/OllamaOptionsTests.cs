// -----------------------------------------------------------------------
// <copyright file="OllamaOptionsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Adapters.Ollama.Tests.Configuration;

/// <summary>
/// Unit tests for <see cref="OllamaOptions"/>.
/// </summary>
public class OllamaOptionsTests
{
    [Fact]
    public void Defaults_TargetLocalhostWithLlama32()
    {
        // Arrange + Act
        var options = new OllamaOptions();

        // Assert
        options.BaseUrl.Should().Be("http://localhost:11434");
        options.DefaultModel.Should().Be("llama3.2");
        options.DefaultEmbeddingModel.Should().Be("nomic-embed-text");
        options.DefaultMaxTokens.Should().Be(2048);
        options.TimeoutSeconds.Should().Be(600);
        options.AutoPullModel.Should().BeFalse();
        options.EnableLogging.Should().BeFalse();
        options.KeepAlive.Should().Be("5m");
    }

    [Fact]
    public void SectionName_MatchesExpected()
    {
        OllamaOptions.SectionName.Should().Be("Ollama");
    }

    [Theory]
    [InlineData("http://localhost:11434", true)]
    [InlineData("https://ollama.internal", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("not-a-url", false)]
    public void IsValid_DependsOnBaseUrl(string baseUrl, bool expected)
    {
        // Arrange
        var options = new OllamaOptions { BaseUrl = baseUrl };

        // Act
        var actual = options.IsValid();

        // Assert
        actual.Should().Be(expected);
    }

    [Fact]
    public void Mutators_AllowFluentReconfiguration()
    {
        // Arrange
        var options = new OllamaOptions();

        // Act
        options.BaseUrl = "http://ollama.local";
        options.DefaultModel = "qwen2.5";
        options.DefaultEmbeddingModel = "mxbai-embed-large";
        options.DefaultMaxTokens = 512;
        options.TimeoutSeconds = 30;
        options.AutoPullModel = true;
        options.EnableLogging = true;
        options.KeepAlive = "30m";

        // Assert
        options.BaseUrl.Should().Be("http://ollama.local");
        options.DefaultModel.Should().Be("qwen2.5");
        options.DefaultEmbeddingModel.Should().Be("mxbai-embed-large");
        options.DefaultMaxTokens.Should().Be(512);
        options.TimeoutSeconds.Should().Be(30);
        options.AutoPullModel.Should().BeTrue();
        options.EnableLogging.Should().BeTrue();
        options.KeepAlive.Should().Be("30m");
    }
}

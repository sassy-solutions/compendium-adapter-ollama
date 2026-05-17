// -----------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensionsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.Ollama.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Compendium.Adapters.Ollama.Tests.DependencyInjection;

/// <summary>
/// Unit tests for <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddCompendiumOllama_WithConfiguration_RegistersProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ollama:BaseUrl"] = "http://example.com:11434",
                ["Ollama:DefaultModel"] = "qwen2.5",
            })
            .Build();

        // Act
        services.AddCompendiumOllama(config);
        using var sp = services.BuildServiceProvider();

        // Assert
        var provider = sp.GetRequiredService<IAIProvider>();
        provider.Should().NotBeNull();
        provider.ProviderId.Should().Be("ollama");

        var options = sp.GetRequiredService<IOptions<OllamaOptions>>().Value;
        options.BaseUrl.Should().Be("http://example.com:11434");
        options.DefaultModel.Should().Be("qwen2.5");
    }

    [Fact]
    public void AddCompendiumOllama_WithInlineConfigure_AppliesOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddCompendiumOllama(opt =>
        {
            opt.BaseUrl = "http://other:11434";
            opt.DefaultModel = "mistral";
            opt.AutoPullModel = true;
        });
        using var sp = services.BuildServiceProvider();

        // Assert
        var options = sp.GetRequiredService<IOptions<OllamaOptions>>().Value;
        options.BaseUrl.Should().Be("http://other:11434");
        options.DefaultModel.Should().Be("mistral");
        options.AutoPullModel.Should().BeTrue();
    }

    [Fact]
    public void AddCompendiumOllama_RegistersIAIProviderAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCompendiumOllama(opt => opt.BaseUrl = "http://localhost:11434");
        using var sp = services.BuildServiceProvider();

        // Act
        var a = sp.GetRequiredService<IAIProvider>();
        var b = sp.GetRequiredService<IAIProvider>();

        // Assert
        a.Should().BeSameAs(b);
    }

    [Fact]
    public void AddCompendiumOllama_WithNullServices_Throws()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act
        var act1 = () => services!.AddCompendiumOllama(_ => { });
        var act2 = () => services!.AddCompendiumOllama(new ConfigurationBuilder().Build());

        // Assert
        act1.Should().Throw<ArgumentNullException>();
        act2.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddCompendiumOllama_WithNullConfigurationOrConfigure_Throws()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act1 = () => services.AddCompendiumOllama((Action<OllamaOptions>)null!);
        var act2 = () => services.AddCompendiumOllama((IConfiguration)null!);

        // Assert
        act1.Should().Throw<ArgumentNullException>();
        act2.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddCompendiumOllama_HonorsTimeoutInHttpClient()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCompendiumOllama(opt =>
        {
            opt.BaseUrl = "http://localhost:11434";
            opt.TimeoutSeconds = 42;
        });

        // Act
        using var sp = services.BuildServiceProvider();
        var factory = sp.GetRequiredService<IHttpClientFactory>();
        using var client = factory.CreateClient(nameof(Compendium.Adapters.Ollama.Http.OllamaHttpClient));

        // Assert
        client.Timeout.Should().Be(TimeSpan.FromSeconds(42));
    }
}

// -----------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensionsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.Ollama.DependencyInjection;
using Compendium.Adapters.Ollama.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Compendium.Adapters.Ollama.Tests.DependencyInjection;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddCompendiumOllamaAdapter_WithConfiguration_RegistersAdapterAndOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Compendium:Adapters:Ollama:BaseUrl"] = "https://api.example.com",
                ["Compendium:Adapters:Ollama:ApiKey"] = "k1",
            })
            .Build();

        // Act
        var actual = services.AddCompendiumOllamaAdapter(configuration);
        var sp = actual.BuildServiceProvider();

        // Assert
        actual.Should().BeSameAs(services);
        sp.GetRequiredService<OllamaAdapter>().Should().NotBeNull();
    }

    [Fact]
    public void AddCompendiumOllamaAdapter_WithCallback_RegistersAdapterAndOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddCompendiumOllamaAdapter(o =>
        {
            o.BaseUrl = "https://api.example.com";
            o.ApiKey = "k1";
        });
        var sp = services.BuildServiceProvider();

        // Assert
        sp.GetRequiredService<OllamaAdapter>().Should().NotBeNull();
    }

    [Fact]
    public void AddCompendiumOllamaAdapter_NullServices_Throws()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act
        var act = () => services!.AddCompendiumOllamaAdapter(_ => { });

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}

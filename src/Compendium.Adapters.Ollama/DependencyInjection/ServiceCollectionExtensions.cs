// -----------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensions.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.Ollama.Configuration;
using Compendium.Adapters.Ollama.Http;
using Compendium.Adapters.Ollama.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Compendium.Adapters.Ollama.DependencyInjection;

/// <summary>
/// DI registration helpers for the Ollama AI provider.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Ollama <see cref="IAIProvider"/> with options bound from
    /// <paramref name="configuration"/> at section <see cref="OllamaOptions.SectionName"/>.
    /// </summary>
    /// <param name="services">The DI container.</param>
    /// <param name="configuration">Configuration root; section <see cref="OllamaOptions.SectionName"/> is bound.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddCompendiumOllama(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<OllamaOptions>(configuration.GetSection(OllamaOptions.SectionName));
        return services.AddCompendiumOllamaCore();
    }

    /// <summary>
    /// Registers the Ollama <see cref="IAIProvider"/> with options configured inline.
    /// </summary>
    /// <param name="services">The DI container.</param>
    /// <param name="configureOptions">Callback to configure <see cref="OllamaOptions"/>.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddCompendiumOllama(
        this IServiceCollection services,
        Action<OllamaOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.Configure(configureOptions);
        return services.AddCompendiumOllamaCore();
    }

    private static IServiceCollection AddCompendiumOllamaCore(this IServiceCollection services)
    {
        services.AddHttpClient<OllamaHttpClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<OllamaOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });
        // Local infrastructure rarely benefits from automatic retries — a single failure
        // usually means a hung model or a missing container, not a transient blip. Callers
        // who want retries can wrap the HttpClient themselves.

        services.AddSingleton<OllamaAIProvider>();
        services.AddSingleton<IAIProvider>(sp => sp.GetRequiredService<OllamaAIProvider>());

        return services;
    }
}

// -----------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensions.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.Ollama.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Compendium.Adapters.Ollama.DependencyInjection;

/// <summary>
/// DI registration helpers for the Ollama adapter.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="OllamaAdapter"/> and its options.
    /// </summary>
    /// <param name="services">DI container.</param>
    /// <param name="configuration">Source configuration; section <see cref="OllamaOptions.SectionName"/> is bound.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddCompendiumOllamaAdapter(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<OllamaOptions>()
            .Bind(configuration.GetSection(OllamaOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<OllamaAdapter>();

        return services;
    }

    /// <summary>
    /// Registers <see cref="OllamaAdapter"/> with an inline configuration callback.
    /// </summary>
    /// <param name="services">DI container.</param>
    /// <param name="configure">Callback to mutate <see cref="OllamaOptions"/>.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddCompendiumOllamaAdapter(
        this IServiceCollection services,
        Action<OllamaOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddOptions<OllamaOptions>()
            .Configure(configure)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<OllamaAdapter>();

        return services;
    }
}

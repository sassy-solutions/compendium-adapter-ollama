// -----------------------------------------------------------------------
// <copyright file="OllamaOptions.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace Compendium.Adapters.Ollama.Options;

/// <summary>
/// Configuration for <see cref="OllamaAdapter"/>.
/// </summary>
public sealed class OllamaOptions
{
    /// <summary>
    /// Configuration section name used by <c>IConfiguration.GetSection(...)</c>.
    /// </summary>
    public const string SectionName = "Compendium:Adapters:Ollama";

    /// <summary>
    /// Vendor base URL. Required.
    /// </summary>
    [Required]
    [Url]
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// API key. Required.
    /// </summary>
    [Required]
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Per-request timeout. Default : 30 seconds.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}

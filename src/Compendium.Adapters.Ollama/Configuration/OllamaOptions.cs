// -----------------------------------------------------------------------
// <copyright file="OllamaOptions.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Adapters.Ollama.Configuration;

/// <summary>
/// Configuration options for the Ollama AI provider.
/// </summary>
/// <remarks>
/// Ollama runs unauthenticated by default on <c>http://localhost:11434</c>. For production
/// use, place it behind a reverse proxy that handles authentication. <strong>Never</strong>
/// expose the raw API to the public internet.
/// </remarks>
public sealed class OllamaOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "Ollama";

    /// <summary>
    /// Gets or sets the base URL of the Ollama HTTP API.
    /// Default is <c>http://localhost:11434</c>.
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:11434";

    /// <summary>
    /// Gets or sets the default model to use when a request omits <see cref="CompletionRequest.Model"/>.
    /// Default is <c>llama3.2</c>.
    /// </summary>
    public string DefaultModel { get; set; } = "llama3.2";

    /// <summary>
    /// Gets or sets the default embedding model.
    /// Default is <c>nomic-embed-text</c>.
    /// </summary>
    public string DefaultEmbeddingModel { get; set; } = "nomic-embed-text";

    /// <summary>
    /// Gets or sets the default maximum tokens hint. Maps to <c>options.num_predict</c>.
    /// Default is <c>2048</c>. Set to <c>-1</c> to let the model generate until natural stop.
    /// </summary>
    public int DefaultMaxTokens { get; set; } = 2048;

    /// <summary>
    /// Gets or sets the HTTP timeout in seconds. Local inference can be slow, especially on first
    /// load — default is <c>600</c> (10 minutes).
    /// </summary>
    public int TimeoutSeconds { get; set; } = 600;

    /// <summary>
    /// Gets or sets a value indicating whether the adapter should auto-pull missing models when
    /// the server returns 404 / model-not-found. Pulling can transfer multi-GB blobs and is
    /// disabled by default. Recommended only for dev environments.
    /// </summary>
    public bool AutoPullModel { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to enable request/response logging at
    /// <see cref="LogLevel.Debug"/>. Useful during dev; never enable in production with secrets.
    /// </summary>
    public bool EnableLogging { get; set; }

    /// <summary>
    /// Gets or sets the <c>keep_alive</c> value forwarded to the Ollama server. Controls how long
    /// the model stays resident in memory. Default <c>5m</c>.
    /// </summary>
    public string KeepAlive { get; set; } = "5m";

    /// <summary>
    /// Validates the options.
    /// </summary>
    /// <returns><c>true</c> when the options can be used to build an HTTP client.</returns>
    public bool IsValid() => !string.IsNullOrWhiteSpace(BaseUrl) && Uri.IsWellFormedUriString(BaseUrl, UriKind.Absolute);
}

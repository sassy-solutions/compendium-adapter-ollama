// -----------------------------------------------------------------------
// <copyright file="OllamaToolCallingExtensions.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.AI.Agents.Models;

namespace Compendium.Adapters.Ollama.Tools;

/// <summary>
/// Ergonomic helpers for attaching tool definitions and reading back tool invocations on
/// the provider-agnostic <see cref="CompletionRequest"/> / <see cref="CompletionResponse"/>.
/// </summary>
/// <remarks>
/// The Compendium abstractions don't carry first-class tool metadata on
/// <see cref="CompletionRequest"/>; this adapter uses well-known keys inside
/// <see cref="CompletionRequest.AdditionalParameters"/> and surfaces tool calls back through
/// <see cref="CompletionResponse.Metadata"/>. Always go through these helpers — the underlying
/// keys are intentionally namespaced and may evolve.
///
/// Tool calling is only supported on models that advertise it (llama3.2+, qwen2.5+, mistral
/// nemo+, etc.). Older models will silently ignore the <c>tools</c> field.
/// </remarks>
public static class OllamaToolCallingExtensions
{
    /// <summary>Key inside <see cref="CompletionRequest.AdditionalParameters"/> carrying the tool list.</summary>
    public const string ToolsKey = "ollama.tools";

    /// <summary>Key inside <see cref="CompletionResponse.Metadata"/> carrying the assistant's tool calls.</summary>
    public const string ToolCallsMetadataKey = "ollama.tool_calls";

    /// <summary>
    /// Attaches a tool catalog to the request. The model may emit one or more
    /// <see cref="AgentToolInvocation"/> entries in <see cref="CompletionResponse.Metadata"/>.
    /// </summary>
    /// <param name="request">The request to clone.</param>
    /// <param name="tools">The tools to expose; ignored when empty.</param>
    /// <returns>A new request with the tools attached.</returns>
    public static CompletionRequest WithTools(
        this CompletionRequest request,
        IReadOnlyList<AgentTool> tools)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(tools);

        var dict = new Dictionary<string, object>(
            request.AdditionalParameters ?? new Dictionary<string, object>())
        {
            [ToolsKey] = tools
        };

        return request with { AdditionalParameters = dict };
    }

    /// <summary>
    /// Reads back tool calls the model requested, if any. Returns an empty list when the model
    /// did not call a tool.
    /// </summary>
    /// <param name="response">The response to inspect.</param>
    /// <returns>The list of tool invocations.</returns>
    public static IReadOnlyList<AgentToolInvocation> GetToolCalls(this CompletionResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        if (response.Metadata != null
            && response.Metadata.TryGetValue(ToolCallsMetadataKey, out var raw)
            && raw is IReadOnlyList<AgentToolInvocation> invocations)
        {
            return invocations;
        }
        return Array.Empty<AgentToolInvocation>();
    }
}

// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.AI;
using Compendium.Abstractions.AI.Models;
using Compendium.Adapters.Ollama.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var services = new ServiceCollection();
services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
services.AddCompendiumOllama(opt =>
{
    opt.BaseUrl = Environment.GetEnvironmentVariable("OLLAMA_BASE_URL") ?? "http://localhost:11434";
    opt.DefaultModel = Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "llama3.2";
    opt.AutoPullModel = true;
});

await using var sp = services.BuildServiceProvider();
var provider = sp.GetRequiredService<IAIProvider>();

// Health-check first so we can print useful guidance when Ollama isn't running.
var health = await provider.HealthCheckAsync();
if (health.IsFailure)
{
    Console.Error.WriteLine("Ollama isn't reachable. Quick setup:");
    Console.Error.WriteLine("  macOS: brew install ollama && brew services start ollama");
    Console.Error.WriteLine("  Linux: curl -fsSL https://ollama.com/install.sh | sh");
    Console.Error.WriteLine("  Docker: docker run -d -p 11434:11434 --name ollama ollama/ollama");
    Console.Error.WriteLine();
    Console.Error.WriteLine($"Then: ollama pull llama3.2");
    Console.Error.WriteLine($"Underlying error: {health.Error.Code} - {health.Error.Message}");
    return 1;
}

Console.WriteLine("Connected to Ollama. Asking 'Tell me a one-line joke.' …");

var request = new CompletionRequest
{
    Model = string.Empty, // use the configured default
    SystemPrompt = "You answer in a single short sentence.",
    Messages = new List<Message> { Message.User("Tell me a one-line joke.") },
    Temperature = 0.7f,
    MaxTokens = 80,
};

var result = await provider.CompleteAsync(request);
if (result.IsFailure)
{
    Console.Error.WriteLine($"Error: {result.Error.Code} - {result.Error.Message}");
    return 1;
}

Console.WriteLine($"Assistant: {result.Value.Content.Trim()}");
Console.WriteLine($"Tokens   : prompt={result.Value.Usage.PromptTokens} completion={result.Value.Usage.CompletionTokens}");
Console.WriteLine($"Finish   : {result.Value.FinishReason}");
return 0;

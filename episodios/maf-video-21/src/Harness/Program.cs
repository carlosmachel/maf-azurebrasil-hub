#pragma warning disable OPENAI001   // Suppress experimental API warnings for Responses API usage.
#pragma warning disable MAAI001     // Suppress experimental API warnings for Agents AI experiments.

using System.ClientModel.Primitives;
using System.Text;
using System.Text.Json;
using Azure.AI.Projects;
using Azure.Identity;
using dotenv.net;
using Harness;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

DotEnv.Load();

var endpoint = Environment.GetEnvironmentVariable("AZURE_AI_PROJECT_ENDPOINT")
    ?? throw new InvalidOperationException("AZURE_AI_PROJECT_ENDPOINT is not set.");
var model = Environment.GetEnvironmentVariable("AZURE_AI_MODEL_DEPLOYMENT_NAME") ?? "gpt-5.4";

const int maxContextWindowTokens = 1_050_000;
const int maxOutputTokens = 128_000;

var instructions =
    """
    ## Vibe Coding Research Assistant

    You are a research assistant specializing in AI-powered developer tools, coding assistants,
    and the vibe coding ecosystem in 2026. When given a topic, research it thoroughly
    using web search and web browsing.

    ### Topics you excel at

    - AI coding assistants: GitHub Copilot, Cursor, Windsurf, Cline, Aider, Continue, Zed AI
    - IDE integrations and MCP (Model Context Protocol) tool ecosystems
    - No-code and low-code AI platforms for developers
    - Vibe coding workflows, benchmarks, and developer productivity studies
    - Pricing, licensing, and enterprise adoption comparisons
    - Open-source vs. proprietary models for code generation

    ### Research quality

    Consult multiple sources and cross-reference key claims.
    When sources disagree, note the discrepancy and explain which you find more reliable and why.
    If a page fails to load or a search returns irrelevant results, try alternative queries.
    Track every source — you will need them when presenting results.

    ### Output format

    Use Markdown with clear headings, bullet points, and inline citations
    (e.g., "According to [source name](url)...").
    When comparing multiple tools, end with a summary table.
    Save the final report to file memory so it persists across sessions.
    """;

AIAgent agent = new AIProjectClient(
    new Uri(endpoint),
    // WARNING: DefaultAzureCredential is convenient for development but requires careful
    // consideration in production. Consider ManagedIdentityCredential instead.
    new DefaultAzureCredential(),
    new AIProjectClientOptions { RetryPolicy = new ClientRetryPolicy(3) })
    .GetProjectOpenAIClient()
    .GetResponsesClient()
    .AsIChatClient(model)
    .AsHarnessAgent(maxContextWindowTokens, maxOutputTokens, new HarnessAgentOptions
    {
        Name = "VibeCodingAgent",
        Description = "Research assistant for AI developer tools and the vibe coding ecosystem.",
        DisableFileAccess = true,
        DisableWebSearch = true,
        FileMemoryStore = new FileSystemAgentFileStore(
            Path.Combine(AppContext.BaseDirectory, "vibe-coding-files")),
        ChatOptions = new ChatOptions
        {
            Instructions = instructions,
            Tools = [new PageFetcher(new PageFetcherOptions { AllowPublicNetworks = true })],
            MaxOutputTokens = maxOutputTokens,
            Reasoning = new() { Effort = ReasoningEffort.Medium },
        },
    });

Console.OutputEncoding = Encoding.UTF8;
Console.ForegroundColor = ConsoleColor.White;
Console.WriteLine("=== Vibe Coding Research Agent ===");
Console.WriteLine("Commands: /exit  /clear");
Console.ResetColor();
Console.WriteLine();

AgentSession session = await agent.CreateSessionAsync();
var currentMode = "execute";

ConsoleColor ModeColor() => currentMode == "plan" ? ConsoleColor.Cyan : ConsoleColor.Green;

while (true)
{
    Console.ForegroundColor = ConsoleColor.White;
    Console.Write("> ");
    Console.ResetColor();

    string? input = Console.ReadLine();

    if (input is null || input.Equals("/exit", StringComparison.OrdinalIgnoreCase))
        break;

    if (input.Equals("/clear", StringComparison.OrdinalIgnoreCase))
    {
        session = await agent.CreateSessionAsync();
        currentMode = "execute";
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("[Session cleared]");
        Console.ResetColor();
        Console.WriteLine();
        continue;
    }

    // When the agent has just presented a plan, pressing Enter with no input approves it.
    if (string.IsNullOrWhiteSpace(input) && currentMode == "plan")
        input = "Looks good. Please proceed with execution.";

    if (string.IsNullOrWhiteSpace(input))
        continue;

    Console.WriteLine();

    var messages = new[] { new ChatMessage(ChatRole.User, input) };
    bool wroteText = false;

    await foreach (var update in agent.RunStreamingAsync(messages, session))
    {
        foreach (var content in update.Contents)
        {
            switch (content)
            {
                case TextContent { Text: { Length: > 0 } text }:
                    Console.ForegroundColor = ModeColor();
                    Console.Write(text);
                    wroteText = true;
                    break;

                case FunctionCallContent { Name: "AgentMode_Set" } call:
                    string? newMode = GetArgString(call, "mode");
                    if (newMode is not null)
                    {
                        currentMode = newMode;
                        Console.ForegroundColor = ModeColor();
                        Console.WriteLine($"\n[Mode: {currentMode}]");
                    }
                    break;

                case FunctionCallContent call:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"\n[Calling: {call.Name}]");
                    break;

                case ErrorContent error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\nError: {error.Message}");
                    break;
            }
        }
    }

    if (wroteText) Console.WriteLine();

    if (currentMode == "plan")
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("[Plan ready — press Enter to approve, or type your feedback]");
    }

    Console.ResetColor();
    Console.WriteLine();
}

Console.ResetColor();
Console.WriteLine("Goodbye!");
return;

// Extracts a string argument from a FunctionCallContent by key name.
// Handles both string and JsonElement argument values.
static string? GetArgString(FunctionCallContent call, string key)
{
    if (call.Arguments?.TryGetValue(key, out var val) != true || val is null)
        return null;
    if (val is JsonElement el && el.ValueKind == JsonValueKind.String)
        return el.GetString();
    return val.ToString();
}

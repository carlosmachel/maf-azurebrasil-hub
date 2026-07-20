// Copyright (c) Microsoft. All rights reserved.

using Azure.AI.Projects;
using Azure.Core;
using Azure.Identity;
using DotNetEnv;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Foundry.Hosting;

// Load .env file if present (for local development without azd).
Env.TraversePath().Load();

// FOUNDRY_PROJECT_ENDPOINT is injected automatically by the Foundry platform once this agent is
// deployed, and by `azd ai agent run` for local development. Don't declare it in azure.yaml's env
// map — see https://learn.microsoft.com/azure/foundry/agents/how-to/author-azure-yaml.
var projectEndpoint = new Uri(Environment.GetEnvironmentVariable("FOUNDRY_PROJECT_ENDPOINT")
    ?? throw new InvalidOperationException("FOUNDRY_PROJECT_ENDPOINT is not set."));

// FOUNDRY_AGENT_NAME is injected automatically once this agent is registered and deployed (it
// matches the `name` set in azure.yaml). For a plain `dotnet run` before deployment, set it
// yourself via .env.
var agentName = Environment.GetEnvironmentVariable("FOUNDRY_AGENT_NAME")
    ?? throw new InvalidOperationException("FOUNDRY_AGENT_NAME is not set.");

// FOUNDRY_MODEL_NAME is a user-declared variable (see azure.yaml's env map) — it is not
// platform-injected, unlike the two variables above.
var deployment = Environment.GetEnvironmentVariable("FOUNDRY_MODEL_NAME") ?? "gpt-4o";

// Use a chained credential: try a temporary dev token first (for local Docker debugging),
// then fall back to DefaultAzureCredential (for local dev via dotnet run / managed identity running in foundry).
TokenCredential credential = new ChainedTokenCredential(
    new DefaultAzureCredential());

// Create the agent via the AI project client using the Responses API.
AIAgent agent = new AIProjectClient(projectEndpoint, credential)
    .AsAIAgent(
        model: deployment,
        instructions: """
            You are a helpful AI assistant hosted as a Foundry Hosted Agent.
            You can help with a wide range of tasks including answering questions,
            providing explanations, brainstorming ideas, and offering guidance.
            Be concise, clear, and helpful in your responses.
            """,
        name: agentName,
        description: "A simple general-purpose AI assistant");

// Host the agent as a Foundry Hosted Agent using the Responses API.
var builder = AgentHost.CreateBuilder(args);
builder.Services.AddFoundryResponses(agent);

// Map the Responses protocol endpoints via the Core SDK's protocol registry
// (mirrors how HostedInvocationAgent registers the Invocations protocol).
builder.RegisterProtocol("responses", endpoints => endpoints.MapFoundryResponses());

var app = builder.Build();

app.Run();

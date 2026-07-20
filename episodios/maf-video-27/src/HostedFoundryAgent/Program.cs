// Copyright (c) Microsoft. All rights reserved.

using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Core;
using Azure.Identity;
using DotNetEnv;
using Microsoft.Agents.AI.Foundry;
using Microsoft.Agents.AI.Foundry.Hosting;

// Load .env file if present (for local development without azd).
Env.TraversePath().Load();

// FOUNDRY_PROJECT_ENDPOINT is injected automatically by the Foundry platform once this agent is
// deployed, and by `azd ai agent run` for local development. Don't declare it in azure.yaml's env
// map — see https://learn.microsoft.com/azure/foundry/agents/how-to/author-azure-yaml.
var projectEndpoint = new Uri(Environment.GetEnvironmentVariable("FOUNDRY_PROJECT_ENDPOINT")
    ?? throw new InvalidOperationException("FOUNDRY_PROJECT_ENDPOINT is not set."));

// PROMPT_AGENT_NAME must name a pre-existing Foundry-managed prompt agent (kind: "prompt") in
// this project — this container wraps and serves it. It must be a different agent from this
// hosted agent itself: the platform's agents/{name} namespace is shared between prompt and
// hosted agents, so pointing this at this container's own name makes it look itself up and call
// its own endpoint, which the platform rejects. Unlike FOUNDRY_-prefixed variables, this name
// isn't reserved by the platform, so it's safe to set via azure.yaml's env map or azd env set.
var agentName = Environment.GetEnvironmentVariable("PROMPT_AGENT_NAME")
    ?? throw new InvalidOperationException("PROMPT_AGENT_NAME is not set.");

// Use a chained credential: try a temporary dev token first (for local Docker debugging),
// then fall back to DefaultAzureCredential (for local dev via dotnet run / managed identity running in foundry).
TokenCredential credential = new ChainedTokenCredential(
    new DefaultAzureCredential());

var aiProjectClient = new AIProjectClient(projectEndpoint, credential);

// Retrieve the Foundry-managed agent by name (latest version).
ProjectsAgentRecord agentRecord = await aiProjectClient
    .AgentAdministrationClient.GetAgentAsync(agentName);

FoundryAgent agent = aiProjectClient.AsAIAgent(agentRecord);

// Host the agent as a Foundry Hosted Agent using the Responses API.
var builder = AgentHost.CreateBuilder(args);
builder.Services.AddFoundryResponses(agent);
builder.RegisterProtocol("responses", endpoints => endpoints.MapFoundryResponses());

var app = builder.Build();

app.Run();

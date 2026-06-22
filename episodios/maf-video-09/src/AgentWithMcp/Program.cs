using System.Text.Json;
using Azure.AI.OpenAI;
using Azure.Identity;
using McpServer;
using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
var settings = new Settings();
builder.Configuration.GetSection("Settings").Bind(settings);

if (string.IsNullOrEmpty(settings.Uri))
    throw new InvalidOperationException("Settings.Uri is not configured in appsettings.json");


// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapScalarApiReference(options =>
{
    options
        .WithTitle("Agent API") 
        .WithTheme(ScalarTheme.Default)
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
});

app.UseHttpsRedirection();

app.MapGet("/microsoft-learn", async (
        [FromQuery] string userInput, 
        [FromQuery] string? serializedSession = null) =>
    {
        var state = string.IsNullOrWhiteSpace(serializedSession)
            ? (JsonElement?)null
            : JsonElement.Parse(serializedSession);

        
        var clientTransport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri("https://learn.microsoft.com/api/mcp"),
            Name = "Microsoft Learn MCP Client"
        });

        var client = await McpClient.CreateAsync(clientTransport);
        
        var tools = await client.ListToolsAsync();

        var agent = new AzureOpenAIClient(
                new Uri(settings.Uri),
                new DefaultAzureCredential())
            .GetChatClient(settings.DeploymentName)
            .AsIChatClient().AsAIAgent(
                instructions: "You are a helpful assistant that can use tools to answer questions about Microsoft Learn.",
                tools: [..tools]);

        AgentSession session;
        if(state is null)
            session = await agent.CreateSessionAsync();
        else 
            session = await agent.DeserializeSessionAsync(state.Value);

        var userMessage = new ChatMessage(ChatRole.User,
            userInput);

        var response = await agent.RunAsync([userMessage], session);
        
        var serializedResponse = await agent.SerializeSessionAsync(session);

        return Results.Ok(new
        {
            response,
            session = serializedResponse
        });
    })
    .WithName("MicrosoftLearn");

app.MapGet("/mcp-server", async (
        [FromQuery] string userInput, 
        [FromQuery] string? serializedSession = null) =>
    {
        var state = string.IsNullOrWhiteSpace(serializedSession)
            ? (JsonElement?)null
            : JsonElement.Parse(serializedSession);

        
        var clientTransport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri("http://localhost:5294/mcp"),
            Name = "Echo Mcp Client"
        });

        var client = await McpClient.CreateAsync(clientTransport);
        
        var tools = await client.ListToolsAsync();

        var agent = new AzureOpenAIClient(
                new Uri(settings.Uri),
                new DefaultAzureCredential())
            .GetChatClient(settings.DeploymentName)
            .AsIChatClient().AsAIAgent(
                instructions: "You are a helpful assistant that can use tools to answer questions about Microsoft Learn.",
                tools: [..tools]);

        AgentSession session;
        if(state is null)
            session = await agent.CreateSessionAsync();
        else 
            session = await agent.DeserializeSessionAsync(state.Value);

        var userMessage = new ChatMessage(ChatRole.User,
            userInput);

        var response = await agent.RunAsync([userMessage], session);
        
        var serializedResponse = await agent.SerializeSessionAsync(session);

        return Results.Ok(new
        {
            response,
            session = serializedResponse
        });
    })
    .WithName("McpServerSample");

app.MapGet("/agent-as-mcp-server", async (
        [FromQuery] string userInput, 
        [FromQuery] string? serializedSession = null) =>
    {
        var state = string.IsNullOrWhiteSpace(serializedSession)
            ? (JsonElement?)null
            : JsonElement.Parse(serializedSession);

        
        var clientTransport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri("https://localhost:7025/mcp"),
            Name = "Agent as MCP Server Client"
        });

        var client = await McpClient.CreateAsync(clientTransport);
        
        var tools = await client.ListToolsAsync();

        var agent = new AzureOpenAIClient(
                new Uri(settings.Uri),
                new DefaultAzureCredential())
            .GetChatClient(settings.DeploymentName)
            .AsIChatClient().AsAIAgent(
                instructions: "You are a helpful assistant that can use tools to answer questions about Microsoft Learn.",
                tools: [..tools]);

        AgentSession session;
        if(state is null)
            session = await agent.CreateSessionAsync();
        else 
            session = await agent.DeserializeSessionAsync(state.Value);

        var userMessage = new ChatMessage(ChatRole.User,
            userInput);

        var response = await agent.RunAsync([userMessage], session);
        
        var serializedResponse = await agent.SerializeSessionAsync(session);

        return Results.Ok(new
        {
            response,
            session = serializedResponse
        });
    })
    .WithName("AgentMcpServer");

app.Run();

using Api.AsMcpTool;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;

var builder = WebApplication.CreateBuilder(args);

var settings = new Settings();
builder.Configuration.GetSection("Settings").Bind(settings);

if (string.IsNullOrEmpty(settings.Uri))
    throw new InvalidOperationException("Settings.Uri is not configured in appsettings.json");

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var agent = new AzureOpenAIClient(
        new Uri(settings.Uri),
        new DefaultAzureCredential())
    .GetChatClient(settings.DeploymentName)
    .AsIChatClient()
    .AsAIAgent(instructions: "You are a helpful assistant");

var tool = McpServerTool.Create(agent.AsAIFunction());

builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithTools(tool);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.Run();
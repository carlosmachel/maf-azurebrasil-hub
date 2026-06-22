using System.ComponentModel;
using Api;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);

// Bind configuration to Settings class
var settings = new Settings();
builder.Configuration.GetSection("Settings").Bind(settings);

// Validate settings
if (string.IsNullOrEmpty(settings.Uri))
    throw new InvalidOperationException("Settings.Uri is not configured in appsettings.json");

// Set up the Azure OpenAI client
var chatClient = new AzureOpenAIClient(
        new Uri(settings.Uri),
        new DefaultAzureCredential())
    .GetChatClient(settings.DeploymentName)
    .AsIChatClient()
    .AsBuilder()
    .Build();

builder.Services.AddChatClient(chatClient);
builder.Services.AddSingleton(settings);

// Define some example tools
[Description("Get the weather for a given location.")]
static string GetWeather([Description("The location to get the weather for.")] string location)
    => $"The weather in {location} is cloudy with a high of 15°C.";

[Description("Calculate the sum of two numbers.")]
static double Add([Description("The first number.")] double a, [Description("The second number.")] double b)
    => a + b;

[Description("Get the current time.")]
static string GetCurrentTime()
    => DateTime.Now.ToString("HH:mm:ss");

// Register sample agents with tools
builder.AddAIAgent("assistant", "You are a helpful assistant. Answer questions concisely and accurately.")
    .WithAITools(
        AIFunctionFactory.Create(GetWeather, name: "get_weather"),
        AIFunctionFactory.Create(GetCurrentTime, name: "get_current_time")
    );

builder.AddAIAgent("poet", "You are a creative poet. Respond to all requests with beautiful poetry.");

builder.AddAIAgent("coder", "You are an expert programmer. Help users with coding questions and provide code examples.")
    .WithAITool(AIFunctionFactory.Create(Add, name: "add"));

// Register sample workflows
var assistantBuilder = builder.AddAIAgent("workflow-assistant", "You are a helpful assistant in a workflow.");
var reviewerBuilder = builder.AddAIAgent("workflow-reviewer", "You are a reviewer. Review and critique the previous response.");
builder.AddWorkflow("review-workflow", (sp, key) =>
{
    var agents = new List<IHostedAgentBuilder>() { assistantBuilder, reviewerBuilder }.Select(ab => sp.GetRequiredKeyedService<AIAgent>(ab.Name));
    return AgentWorkflowBuilder.BuildSequential(workflowName: key, agents: agents);
}).AddAsAIAgent();

builder.Services.AddOpenAIResponses();
builder.Services.AddOpenAIConversations();

var app = builder.Build();

app.MapOpenAIResponses();
app.MapOpenAIConversations();

if (builder.Environment.IsDevelopment())
{
    app.MapDevUI();
}

Console.WriteLine("DevUI is available at: https://localhost:7067/devui");
Console.WriteLine("Press Ctrl+C to stop the server.");

app.Run();
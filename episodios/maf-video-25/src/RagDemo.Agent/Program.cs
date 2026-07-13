using System.ClientModel;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;
using OpenAI.Chat;
using RagDemo.Agent;
using RagDemo.Core;

var settings = RagDemoSettings.Load();
await settings.ApplyMigrationsAsync();

var azureClient = new AzureOpenAIClient(
    new Uri(settings.AzureOpenAi.Endpoint),
    new DefaultAzureCredential());

IEmbeddingService embeddingService = new AzureEmbeddingService(azureClient, settings.AzureOpenAi.EmbeddingDeployment);
IVectorStore vectorStore = new EfVectorStore(settings.SqlConnectionString);
IPermissionService permissionService = new PermissionService(vectorStore, new MemoryCache(new MemoryCacheOptions()));

var agent = azureClient
    .GetChatClient(settings.AzureOpenAi.ChatDeployment)
    .AsAIAgent(new ChatClientAgentOptions
    {
        ChatOptions = new ChatOptions
        {
            Instructions = "You are a helpful internal assistant. Answer strictly using the retrieved context provided for the current user, and always cite the source file name. If no context is available, say plainly that you don't have access to that information."
        },
        AIContextProviders = [new DocumentContextProvider(permissionService, vectorStore, embeddingService)]
    });

Console.WriteLine("Permission-Aware RAG Demo (SQL Server VECTOR + Microsoft Agent Framework)");
Console.WriteLine("Known users: user-a (Project Phoenix), user-b (Project Atlas)");

var userId = PromptUserId();
var session = await agent.CreateSessionAsync();
session.StateBag.SetValue("UserId", userId);
Console.WriteLine($"Signed in as '{userId}'. Ask a question, type 'switch' to change user, or 'exit' to quit.");

while (true)
{
    Console.Write("\n> ");
    var input = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(input))
    {
        continue;
    }

    if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    if (input.Equals("switch", StringComparison.OrdinalIgnoreCase))
    {
        userId = PromptUserId();
        session = await agent.CreateSessionAsync();
        session.StateBag.SetValue("UserId", userId);
        Console.WriteLine($"Signed in as '{userId}'.");
        continue;
    }

    var response = await agent.RunAsync(input, session);
    Console.WriteLine(response.Text);
}

return;

static string PromptUserId()
{
    while (true)
    {
        Console.Write("User id (user-a / user-b): ");
        var userId = Console.ReadLine()?.Trim();
        if (!string.IsNullOrWhiteSpace(userId))
        {
            return userId;
        }
    }
}



using Azure.AI.OpenAI;
using Azure.Identity;
using dotenv.net;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.InMemory;
using OpenAI.Chat;

DotEnv.Load();

var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set.");
var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-4o-mini";
var embeddingDeploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_EMBEDDING_DEPLOYMENT_NAME") ?? "text-embedding-3-large";

VectorStore vectorStore = new InMemoryVectorStore(new InMemoryVectorStoreOptions()
{
    EmbeddingGenerator = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
        .GetEmbeddingClient(embeddingDeploymentName)
        .AsIEmbeddingGenerator()
});

AIAgent agent = new AzureOpenAIClient(
        new Uri(endpoint),
        new DefaultAzureCredential())
    .GetChatClient(deploymentName)
    .AsAIAgent(new ChatClientAgentOptions()
    {
        ChatOptions = new()
        {
            Instructions = "You are good at telling jokes."
        },
        Name = "Joker",
        AIContextProviders =
        [
            new ChatHistoryMemoryProvider(
                vectorStore,
                collectionName: "chat_history",
                vectorDimensions: 3072,
                session => new ChatHistoryMemoryProvider.State(
                    storageScope: new ChatHistoryMemoryProviderScope
                    {
                        UserId = "Unico", SessionId = session?.GetHashCode().ToString() ?? ""
                    },
                    searchScope: new ChatHistoryMemoryProviderScope {  UserId = "Unico" }))]
    });
    
    AgentSession session = await agent.CreateSessionAsync();
    Console.WriteLine("=== Session 1 ===");
    
    Console.WriteLine(await agent.RunAsync("I like jokes about pirate. Tell me a joke about a pirate.", session));
    
    AgentSession session2 = await agent.CreateSessionAsync();
    Console.WriteLine("=== Session 2 ===");
    
    Console.WriteLine(await agent.RunAsync("Tell me a joke that I might like.", session2));
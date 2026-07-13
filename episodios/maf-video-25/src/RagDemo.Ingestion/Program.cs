using Azure.AI.OpenAI;
using Azure.Identity;
using RagDemo.Core;

if (args.Length != 3)
{
    Console.WriteLine("Usage: dotnet run --project RagDemo.Ingestion -- <path-to-txt> <fileId> <ownerUserId>");
    return 1;
}

var filePath = args[0];
var fileId = args[1];
var ownerUserId = args[2];

if (!File.Exists(filePath))
{
    Console.Error.WriteLine($"File not found: {filePath}");
    return 1;
}

var settings = RagDemoSettings.Load();
await settings.ApplyMigrationsAsync();

var text = await File.ReadAllTextAsync(filePath);
var chunks = ChunkingService.Chunk(text);
Console.WriteLine($"Split '{Path.GetFileName(filePath)}' into {chunks.Count} chunk(s).");

var azureClient = new AzureOpenAIClient(
    new Uri(settings.AzureOpenAi.Endpoint),
    new DefaultAzureCredential());

IEmbeddingService embeddingService = new AzureEmbeddingService(azureClient, settings.AzureOpenAi.EmbeddingDeployment);

var embeddedChunks = new List<(string Content, float[] Embedding)>();
foreach (var chunk in chunks)
{
    var embedding = await embeddingService.EmbedAsync(chunk);
    embeddedChunks.Add((chunk, embedding));
    Console.WriteLine($"  embedded chunk {embeddedChunks.Count}/{chunks.Count}");
}

IVectorStore vectorStore = new EfVectorStore(settings.SqlConnectionString);

var fileName = Path.GetFileName(filePath);
await vectorStore.UpsertFileAsync(new FileRecord(fileId, fileName, filePath));
await vectorStore.ReplaceChunksAsync(fileId, embeddedChunks);
await vectorStore.GrantAccessAsync(ownerUserId, fileId);

Console.WriteLine($"Ingested '{fileName}' as file '{fileId}' and granted access to user '{ownerUserId}'.");
return 0;

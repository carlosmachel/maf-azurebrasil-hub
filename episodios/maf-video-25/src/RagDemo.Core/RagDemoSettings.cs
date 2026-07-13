using Microsoft.Extensions.Configuration;

namespace RagDemo.Core;

public sealed record AzureOpenAiSettings(string Endpoint, string ChatDeployment, string EmbeddingDeployment);

public sealed record RagDemoSettings(string SqlConnectionString, AzureOpenAiSettings AzureOpenAi)
{
    public static RagDemoSettings Load()
    {
        var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";

        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = Require(configuration, "ConnectionStrings:DefaultConnection");

        var azureOpenAi = new AzureOpenAiSettings(
            Endpoint: Require(configuration, "AZURE_OPENAI_ENDPOINT"),
            ChatDeployment: Require(configuration, "AZURE_OPENAI_CHAT_DEPLOYMENT"),
            EmbeddingDeployment: Require(configuration, "AZURE_OPENAI_EMBEDDING_DEPLOYMENT"));

        return new RagDemoSettings(connectionString, azureOpenAi);
    }

    private static string Require(IConfiguration configuration, string key)
    {
        var value = configuration[key];
        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"Required configuration value '{key}' is not set.")
            : value;
    }
}

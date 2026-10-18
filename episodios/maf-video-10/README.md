# Ep. 10 — Long-term memory with `ChatHistoryMemoryProvider`

> Part of the **Microsoft Agent Framework** video series on [AzureBrasil.Cloud](https://azurebrasil.cloud) (PT-BR). All episodes: [maf-azurebrasil-hub](https://carlosmachel.github.io/maf-azurebrasil-hub/)

An agent that remembers user preferences **across sessions**. Every message is embedded and stored in a vector store; when a new session starts, the provider runs a semantic search over the user's past conversations and injects the relevant snippets into the context.

## What it shows

- `ChatHistoryMemoryProvider` plugged into an agent via `AIContextProviders`
- **Storage scope vs. search scope**: messages are stored per `UserId` + `SessionId`, but searched per `UserId` only — so memory crosses session boundaries
- `InMemoryVectorStore` with Azure OpenAI embeddings (`text-embedding-3-large`, 3072 dims)

```
Session 1: "I like jokes about pirates. Tell me one."   → stored + embedded
Session 2: "Tell me a joke that I might like."           → search finds "pirates" → pirate joke
```

## Stack

.NET 10 · `Microsoft.Agents.AI` · `Microsoft.Agents.AI.OpenAI` · `Microsoft.SemanticKernel.Connectors.InMemory` · Azure OpenAI · `DefaultAzureCredential`

## Run

```bash
az login
cd src/ChatHistoryMemoryProviderDemo
cat > .env <<'ENV'
AZURE_OPENAI_ENDPOINT=https://<your-resource>.openai.azure.com/
AZURE_OPENAI_DEPLOYMENT_NAME=gpt-4o-mini
AZURE_OPENAI_EMBEDDING_DEPLOYMENT_NAME=text-embedding-3-large
ENV
dotnet run
```

## Going further

Swap `InMemoryVectorStore` for a persistent connector (Azure AI Search, SQL Server, Postgres/pgvector) to keep memory across process restarts.

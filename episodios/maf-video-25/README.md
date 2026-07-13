# Permission-Aware RAG with SQL Server VECTOR + Microsoft Agent Framework

This demo shows how to build a RAG agent whose retrieval is filtered by per-user file permissions. Each user only receives answers grounded in documents they have access to — enforced at the SQL query level using the native `VECTOR` type in SQL Server 2025, with an optional in-memory caching layer that can be invalidated on demand.

## Architecture

```
user query
   │
   ▼
DocumentContextProvider (AIContextProvider)
   │  reads userId from AgentSession.StateBag
   │  checks PermissionService (30s cache, instant invalidation on revoke)
   │  embeds query via Azure OpenAI
   │  runs VECTOR_DISTANCE search with SQL-level permission join
   │  injects retrieved chunks as Instructions into AIContext
   ▼
MAF ChatClientAgent
   │  sends messages + injected context to Azure OpenAI Chat
   ▼
cited answer, grounded in user-accessible documents only
```

## Prerequisites

- Docker Desktop
- .NET 10 SDK
- An Azure OpenAI resource with:
  - A **chat** deployment (e.g. `gpt-4o`)
  - An **embeddings** deployment (e.g. `text-embedding-3-small` — must produce 1536-dimension vectors)

## Setup

### 1. Configuration

The SQL Server container only needs its `sa` password, supplied via `.env`:

```sh
cp .env.example .env
# then edit .env
```

| Variable | Description |
|---|---|
| `MSSQL_SA_PASSWORD` | SA password for the local SQL Server container (docker-compose only) |

The apps (`RagDemo.Agent` / `RagDemo.Ingestion`) read their settings from `appsettings.json` + `appsettings.Development.json` (layered, `DOTNET_ENVIRONMENT` defaults to `Development`), not from `.env`. `appsettings.Development.json` is gitignored — copy the placeholder keys from `appsettings.json` into it and fill in real values:

| Key | Description |
|---|---|
| `ConnectionStrings:DefaultConnection` | Full SQL Server connection string, e.g. `Server=localhost,1433;Database=RagDemo;User Id=sa;Password=...;TrustServerCertificate=True;Encrypt=False;` (matches the `sa` password in `.env`) |
| `AZURE_OPENAI_ENDPOINT` | Azure OpenAI endpoint URL |
| `AZURE_OPENAI_CHAT_DEPLOYMENT` | Chat model deployment name |
| `AZURE_OPENAI_EMBEDDING_DEPLOYMENT` | Embeddings deployment name (1536 dims) |

Authentication to Azure OpenAI uses `DefaultAzureCredential` (e.g. `az login`), not an API key.

### 2. Start SQL Server

```sh
docker compose up -d
```

### 3. Ingest the sample documents

Run the ingestion tool. It applies any pending EF Core migrations on startup, so the `RagDemo` database, `Files`, `DocumentChunks` (VECTOR column), and `FilePermissions` tables are created automatically the first time it runs:

```sh
dotnet run --project src/RagDemo.Ingestion -- samples/user-a-notes.txt user-a-notes user-a
dotnet run --project src/RagDemo.Ingestion -- samples/user-b-notes.txt user-b-notes user-b
```

Usage: `<path-to-file> <fileId> <ownerUserId>`

- `user-a-notes.txt` is ingested as file ID `user-a-notes` and granted to `user-a`.
- `user-b-notes.txt` is ingested as file ID `user-b-notes` and granted to `user-b`.

### 4. Run the agent

```sh
dotnet run --project src/RagDemo.Agent
```

You will be prompted to choose a user. Ask questions and see how answers are scoped to what each user can see.

## Demo scenarios

See [demo-script.md](demo-script.md) for a step-by-step recording guide.

## Project structure

```
samples/
  user-a-notes.txt        — Project Phoenix notes (user-a only)
  user-b-notes.txt        — Project Atlas notes (user-b only)
src/
  RagDemo.Core/
    Entities/                    — FileEntity, DocumentChunkEntity, FilePermissionEntity
    Migrations/                  — EF Core migrations (InitialCreate: Files, DocumentChunks VECTOR, FilePermissions)
    RagDemoDbContext.cs          — EF Core model, vector(1536) column mapping
    RagDemoDbContextFactory.cs   — DbContextOptions builder + design-time factory for `dotnet ef`
    RagDemoSettingsExtensions.cs — ApplyMigrationsAsync(), called on startup by Agent/Ingestion
    Models.cs                    — FileRecord, RetrievedChunk records
    ChunkingService.cs           — sentence-aware text splitter
    EmbeddingService.cs          — wraps AzureOpenAIClient.GetEmbeddingClient
    EfVectorStore.cs             — EF.Functions.VectorDistance search + permission JOIN
    PermissionService.cs         — 30s cache + explicit invalidation on grant/revoke
    RagDemoSettings.cs           — configuration from appsettings.json + env vars
  RagDemo.Ingestion/      — applies migrations, chunk → embed → upsert → grant access
  RagDemo.Agent/
    DocumentContextProvider.cs  — AIContextProvider: embeds query, retrieves, injects
    Program.cs                  — applies migrations, then runs the interactive console demo loop
```

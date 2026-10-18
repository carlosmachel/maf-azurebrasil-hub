# Ep. 19 — Code Interpreter with Microsoft Foundry

> Part of the **Microsoft Agent Framework** video series on [AzureBrasil.Cloud](https://azurebrasil.cloud) (PT-BR). All episodes: [maf-azurebrasil-hub](https://carlosmachel.github.io/maf-azurebrasil-hub/)

A **revenue report agent**: it calls a C# function to fetch monthly revenue, then uses the hosted **Code Interpreter** to build a matplotlib chart, a CSV and a PDF report (reportlab). The program downloads the generated files from the Code Interpreter container.

## What it shows

- `AIProjectClient.AsAIAgent(...)` with `HostedCodeInterpreterTool` + a local `AIFunction`
- Mixing **local tools** (data access) with **hosted tools** (sandboxed Python)
- Reading `ContainerFileCitationMessageAnnotation` from the response and downloading files with the container client
- Safe file names on download (`Path.GetFileName` to prevent path traversal)

## Stack

.NET 10 · `Microsoft.Agents.AI.Foundry` · `Azure.AI.Projects` · Microsoft Foundry · `DefaultAzureCredential`

## Run

```bash
az login
cp .env.example src/CodeInterpreter/.env   # fill in the values
cd src/CodeInterpreter
dotnet run
```

| Variable | Example |
|----------|---------|
| `AZURE_AI_PROJECT_ENDPOINT` | `https://<resource>.services.ai.azure.com/api/projects/<project>` |
| `AZURE_AI_MODEL_DEPLOYMENT_NAME` | `gpt-4o-mini` |

The generated chart, CSV and PDF are saved in the current folder.

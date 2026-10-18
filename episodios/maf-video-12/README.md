# Ep. 12 — Reasoning models + tools (multi-hop)

> Part of the **Microsoft Agent Framework** video series on [AzureBrasil.Cloud](https://azurebrasil.cloud) (PT-BR). All episodes: [maf-azurebrasil-hub](https://carlosmachel.github.io/maf-azurebrasil-hub/)

An HR assistant that uses a **reasoning model through the Responses API** and chains several tool calls to answer questions that no single tool can answer by itself.

## What it shows

- Enabling reasoning in `Microsoft.Extensions.AI`: `ReasoningOptions { Effort = Medium, Output = Full }`
- `UseFunctionInvocation()` middleware running C# tools automatically
- **Multi-hop**: the result of one tool feeds the next one
  - *"Is Julia's salary fair compared to her department's average?"* → `GetEmployeeDetail` → find department → `GetAverageSalaryByDepartment` → compare → recommendation
- Ambiguous questions where the model has to **plan** which data to fetch
- Streaming vs. non-streaming, and token usage per run

## Tools

| Tool | What it does |
|------|--------------|
| `SearchEmployees` | Filter by department and minimum tenure |
| `GetAverageSalaryByDepartment` | Average / min / max salary and headcount |
| `GetEmployeeDetail` | Full profile by ID |
| `ListDepartments` | Available departments |

Data comes from an in-memory fake database (`FakeDb.cs`).

## Stack

.NET 10 · `Microsoft.Agents.AI` · Azure OpenAI Responses API · `DefaultAzureCredential`

## Run

```bash
az login
cd src/Reasoning
cat > .env <<'ENV'
AZURE_OPENAI_ENDPOINT=https://<your-resource>.openai.azure.com/
AZURE_OPENAI_DEPLOYMENT_NAME=gpt-5.4-mini
ENV
dotnet run
```

Use a reasoning-capable deployment (gpt-5.x / o-series).

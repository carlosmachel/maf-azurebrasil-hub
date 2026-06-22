# maf-video-21 — Vibe Coding Research Agent

A console-based AI agent built on **Microsoft Agents AI** and **Azure AI Foundry** that researches AI developer tools and the vibe coding ecosystem. The agent streams responses, supports plan/execute modes, persists file memory across sessions, and can fetch and convert web pages to Markdown on demand.

## Architecture

```
[User] → [AIAgent / Harness] → [PageFetcher] → [Web] → [Response]
```

### Middleware pipeline

```
IChatClient (your model)
↓ FunctionInvokingChatClient   — executes tools automatically
↓ MessageInjectingChatClient   — injects messages mid-stream
↓ PerServiceCallChatHistoryPersisting — persists history after each call
↓ AIContextProviderChatClient  — applies context compaction before each call
```

### Context providers

| Provider | What it does |
|---|---|
| `AgentModeProvider` | Tracks plan / execute mode |
| `FileMemoryProvider` | Session memory stored as files |
| `TodoProvider` | Agent-internal task list |
| `FileAccessProvider` | File access inside `./working/` |

## NuGet packages

| Package | Version | Purpose |
|---|---|---|
| `Azure.AI.Projects` | 2.1.0-beta.3 | Azure AI Foundry project client |
| `Microsoft.Agents.AI` | 1.8.0 | Core agent abstractions (`AIAgent`, `AgentSession`) |
| `Microsoft.Agents.AI.Foundry` | 1.8.0-preview | Azure Foundry integration |
| `Microsoft.Agents.AI.Harness` | 1.8.0-preview | Harness builder and providers |
| `dotenv.net` | 4.0.2 | `.env` file loading |
| `OpenTelemetry` | 1.15.3 | Observability |
| `OpenTelemetry.Exporter.Console` | 1.15.3 | Console telemetry exporter |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- An [Azure AI Foundry](https://ai.azure.com) project with a deployed model
- Azure credentials configured locally (`az login` or environment variables)

## Setup

1. Copy the sample env file and fill in your values:

```bash
cp src/Harness/.env.sample src/Harness/.env
```

```env
AZURE_AI_PROJECT_ENDPOINT=https://<your-project>.services.ai.azure.com/api/projects/<project-id>
AZURE_AI_MODEL_DEPLOYMENT_NAME=gpt-5.4
```

2. Authenticate with Azure:

```bash
az login
```

## How to run

```bash
dotnet run --project src/Harness
```

The agent starts an interactive session in your terminal:

```
=== Vibe Coding Research Agent ===
Commands: /exit  /clear

> Research the latest GitHub Copilot pricing changes
```

### Commands

| Command | What it does |
|---|---|
| `<message>` | Send a message to the agent |
| `/clear` | Start a fresh session (clears history) |
| `/exit` | Quit the program |
| `Enter` (in plan mode) | Approve the current plan and proceed to execution |

### Plan / Execute mode

The agent automatically switches between **plan** (cyan) and **execute** (green) modes. When a plan is ready, pressing Enter with no input approves it.

## How `PageFetcher` works

`PageFetcher` is an `AIFunction` tool the agent can call to download and read web pages. It resolves the target host, enforces network access policy, fetches the HTML, and converts it to clean Markdown — no external parsing libraries required.

### Network access policy (`PageFetcherOptions`)

| Option | Default | Description |
|---|---|---|
| `AllowedHosts` | `null` | Whitelist of host patterns (supports `*.domain.com`) |
| `AllowPublicNetworks` | `false` | Permit public internet access |
| `AllowPrivateNetworks` | `false` | Permit private / loopback networks (SSRF risk) |
| `AllowAllHosts` | `false` | Bypass all checks (isolated environments only) |

In this sample, `AllowPublicNetworks = true` is set so the agent can browse the web.

## Contributing

1. Fork the repository and create a feature branch:

```bash
git checkout -b feat/your-feature
```

2. Restore dependencies and verify the build:

```bash
dotnet restore
dotnet build
```

3. Make your changes, then run the agent locally to validate end-to-end behaviour:

```bash
dotnet run --project src/Harness
```

4. Open a pull request against `main` with a clear description of what changed and why.

### Code style

- Target **C# 13 / .NET 10**
- Nullable reference types are enabled — keep everything annotated
- Prefer `AIFunctionFactory.Create` over manual tool wiring
- Network-facing code must go through `PageFetcherOptions` policy checks — never bypass SSRF guards

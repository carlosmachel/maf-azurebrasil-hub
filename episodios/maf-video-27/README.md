# Ep. 27 — Foundry Hosted Agents (.NET)

> Part of the **Microsoft Agent Framework** video series on [AzureBrasil.Cloud](https://azurebrasil.cloud) (PT-BR). All episodes: [maf-azurebrasil-hub](https://carlosmachel.github.io/maf-azurebrasil-hub/)

Three ways to run your own container as a **Foundry Hosted Agent**, each deployable with `azd`. Each project has its own README with run and deploy steps.

| Project | Protocol | Where the agent is defined | Needs an LLM |
|---------|----------|----------------------------|--------------|
| [`HostedInvocationAgent`](src/HostedInvocationAgent) | Invocations | Code (`EchoAIAgent`) | No |
| [`HostedChatClientAgent`](src/HostedChatClientAgent) | Responses | Code — model + instructions in `Program.cs` | Yes |
| [`HostedFoundryAgent`](src/HostedFoundryAgent) | Responses | Foundry — the container serves an existing prompt agent | Yes |

## What it shows

- `AgentHost` + protocol registration (`/invocations`, `/responses`, `/readiness`)
- Platform-injected variables (`FOUNDRY_PROJECT_ENDPOINT`, `FOUNDRY_AGENT_NAME`) vs. user-declared ones in `azure.yaml`
- Wrapping a platform-managed prompt agent vs. defining the agent in code
- Deploying with `azd provision` / `azd deploy` using the current `azure.yaml` schema
- `DefaultAzureCredential` locally, managed identity in Foundry

## Prerequisites

- .NET 10 SDK, Docker
- A Microsoft Foundry project with a model deployment
- `az login` and `azd auth login`

## Quick start

```bash
cd src/HostedChatClientAgent
cp .env.sample .env      # fill in FOUNDRY_PROJECT_ENDPOINT
dotnet run               # http://localhost:8088
curl -X POST http://localhost:8088/responses -H "Content-Type: application/json" -d '{"input":"Hello!"}'
```

# maf-video-20 — Microsoft Foundry Memory

Demonstrates how an AI agent can learn a user's writing style from example LinkedIn posts and reproduce it faithfully — even across completely new sessions — using **Microsoft Foundry Memory**.

## What it does

The demo runs a LinkedIn Post Agent through three sessions:

1. **Session 1 — Learning**: the agent receives two real LinkedIn posts and memorizes the user's style (tone, length, structure, opening/closing patterns).
2. **Session 2 — New session, preserved style**: a brand-new session is created with no shared conversation history. The agent still generates a post in the exact learned style, thanks to the persisted memory store.
3. **Session 3 — Refinement**: another new session where the agent creates a post and then refines it based on a follow-up instruction, still honoring the stored style.

## Tech stack

| Package | Version |
|---|---|
| .NET | 10.0 |
| `Azure.AI.Projects` | 2.1.0-beta.2 |
| `Azure.Identity` | 1.21.0 |
| `Microsoft.Agents.AI` | 1.7.0 |
| `Microsoft.Agents.AI.Foundry` | 1.7.0-preview.260526.1 |
| `dotenv.net` | 4.0.2 |

## Prerequisites

- An Azure AI Foundry project
- A deployed chat model (e.g. `gpt-4o-mini`) and an embedding model (e.g. `text-embedding-3-large`)
- Azure CLI authenticated (`az login`)

## Configuration

Create a `.env` file inside `src/FoundryMemory/`:

```env
AZURE_AI_PROJECT_ENDPOINT=https://<your-project>.services.ai.azure.com/api/projects/<your-project>
AZURE_AI_MEMORY_STORE_ID=linkedin-memory-store-video
AZURE_AI_MODEL_DEPLOYMENT_NAME=gpt-4o-mini
AZURE_AI_EMBEDDING_DEPLOYMENT_NAME=text-embedding-3-large
```

## Running

```bash
cd src/FoundryMemory
dotnet run
```

## Key concept

`FoundryMemoryProvider` is scoped to a fixed user identifier (`linkedin-user-002`). Any information the agent saves during Session 1 is written to the Foundry Memory Store and automatically retrieved in Sessions 2 and 3 — demonstrating true cross-session, long-term agent memory.

# Hosted-Invocations-EchoAgent

A minimal echo agent hosted as a Foundry Hosted Agent using the **Invocations protocol**. The agent reads the request body as plain text, passes it through a custom `EchoAIAgent`, and writes the echoed text back in the response. No LLM or Azure credentials are required — this sample exists purely to show the host mechanics.

> This is a video-companion copy of [`dotnet/samples/04-hosting/FoundryHostedAgents/invocations/Hosted-Invocations-EchoAgent`](../../../dotnet/samples/04-hosting/FoundryHostedAgents/invocations/Hosted-Invocations-EchoAgent/), updated to the current `azure.yaml` deployment schema (the platform-level docs mark the old `agent.yaml`/`agent.manifest.yaml` as deprecated).

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

## Running directly

```bash
cd foundry-hosted-agents-videos/samples/Hosted-Invocations-EchoAgent
dotnet run
```

The agent starts on `http://localhost:5000`.

### Test it

```bash
curl -X POST http://localhost:8088/invocations -H 'Content-Type: text/plain' -d 'Hello, world!'
```

Expected response:

```
Echo: Hello, world!
```

## Deploying with azd

`azure.yaml` already exists in this folder, so there's no need to run `azd ai agent init` —
that command scaffolds a new `azure.yaml` and would overwrite this one. Just point `azd` at the
existing Foundry project and provision/deploy directly:

```bash
azd auth login
azd env set AZURE_EXISTING_AIPROJECT_ENDPOINT <your-foundry-project-endpoint>
azd provision   # connects to the existing project above instead of creating a new one
azd deploy
```

No app-level environment variables are required — this agent doesn't call a model. `azure.yaml`'s
`ai-project.endpoint` field is what tells `azd provision` which existing Foundry project to deploy
this agent into (the same project can host `Hosted-ChatClientAgent` and `Hosted-FoundryAgent` too —
they don't need to be separate projects). If you'd rather have `azd provision` create a brand-new,
empty project just for this agent, remove the `endpoint: ${AZURE_EXISTING_AIPROJECT_ENDPOINT}` line
from `azure.yaml` and skip the `azd env set` step.

> **Not yet verified against a real `azd`/Azure subscription.** The `azure.yaml` here matches the
> documented schema ([azure.yaml reference for hosted agents](https://learn.microsoft.com/azure/foundry/agents/concepts/azure-yaml-reference)),
> but only `dotnet run` + local `curl` was actually exercised end-to-end while building this sample.
> Run the steps above yourself and check the output before recording — `azd` may still prompt for
> fields (subscription, region) not pinned here.

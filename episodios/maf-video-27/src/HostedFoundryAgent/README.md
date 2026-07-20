# Hosted-FoundryAgent

A Foundry Hosted Agent using the **Responses protocol**, same as `HostedChatClientAgent` — but here
the agent is **not** defined in code. `Program.cs` fetches an already-registered, platform-managed
prompt agent by name (`PROMPT_AGENT_NAME`) via `AgentAdministrationClient.GetAgentAsync`, and this
container only serves it. Model, instructions, and tools all live in Foundry (UI/CLI/API), not in
this repo.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- A **pre-existing prompt agent** already registered in the target Foundry project (created via
  Foundry UI, CLI, or API) — this container is just the transport, not the agent definition
- `az login`/`azd auth login` credentials (`DefaultAzureCredential`)

## Running directly

```bash
cd src/HostedFoundryAgent
cp .env.sample .env   # fill in FOUNDRY_PROJECT_ENDPOINT and PROMPT_AGENT_NAME
dotnet run
```

The agent starts on `http://localhost:8088`.

### Test it

```bash
curl -X POST http://localhost:8088/responses \
  -H "Content-Type: application/json" \
  -d '{"input": "Hello!"}'
```

## Deploying with azd

`azure.yaml` already exists in this folder, so there's no need to run `azd ai agent init` —
that command scaffolds a new `azure.yaml` and would overwrite this one. Just point `azd` at the
existing Foundry project and provision/deploy directly:

```bash
azd auth login
azd env set AZURE_EXISTING_AIPROJECT_ENDPOINT <your-foundry-project-endpoint>
azd env set PROMPT_AGENT_NAME <name-of-an-existing-prompt-agent-in-the-project>
azd provision   # connects to the existing project above instead of creating a new one
azd deploy
```

> **⚠️ Alerta — não verificado / cuidados com nomes:**
> - **Não testado ainda contra uma assinatura Azure real.** O `azure.yaml` segue o schema
>   documentado ([azure.yaml reference for hosted agents](https://learn.microsoft.com/azure/foundry/agents/concepts/azure-yaml-reference)),
>   mas só `dotnet run` + `curl` local foram exercitados de fato até agora. Rode os passos acima
>   você mesmo e confira a saída antes de gravar — `azd` pode pedir campos (subscription, região)
>   que não estão fixados aqui.
> - **`PROMPT_AGENT_NAME` precisa apontar pra um agente *diferente* deste hosted agent.** O
>   namespace `agents/{name}` da plataforma é compartilhado entre prompt agents e hosted agents —
>   se `PROMPT_AGENT_NAME` for igual ao `name: hosted-foundry-agent` deste próprio serviço, o
>   container acaba chamando a si mesmo, e a plataforma rejeita a chamada (ver comentário em
>   `Program.cs`).
> - **`AZURE_AI_MODEL_DEPLOYMENT_NAME` no `env` map do `azure.yaml` não é lido por este código.**
>   `Program.cs` não declara nenhuma variável de deployment de modelo — o modelo já vem configurado
>   no prompt agent existente (`PROMPT_AGENT_NAME`) lá na plataforma. Essa entrada parece um
>   copy-paste sobrando de `HostedChatClientAgent/azure.yaml` e provavelmente pode ser removida.

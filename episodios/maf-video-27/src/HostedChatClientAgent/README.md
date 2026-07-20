# Hosted-ChatClientAgent

A Foundry Hosted Agent using the **Responses protocol**, with the agent defined **inline in
code** — model, instructions, and name all live in `Program.cs` (`AsAIAgent(model, instructions, ...)`).
No pre-existing agent needs to be registered on the platform; only a model deployment.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- An Azure AI Foundry project with a model deployment (e.g. `gpt-4o`) and `az login`/`azd auth login`
  credentials (`DefaultAzureCredential`)

## Running directly

```bash
cd src/HostedChatClientAgent
cp .env.sample .env   # fill in FOUNDRY_PROJECT_ENDPOINT
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
azd env set AZURE_AI_MODEL_DEPLOYMENT_NAME <your-model-deployment-name>
azd provision   # connects to the existing project above instead of creating a new one
azd deploy
```

Or run locally through `azd` instead of `dotnet run` — it injects `FOUNDRY_PROJECT_ENDPOINT` and
`FOUNDRY_AGENT_NAME` automatically, so no `.env` is needed:

```bash
azd ai agent run
```

> **⚠️ Alerta — não verificado / possível divergência de variável:**
> - **Não testado ainda contra uma assinatura Azure real.** O `azure.yaml` segue o schema
>   documentado ([azure.yaml reference for hosted agents](https://learn.microsoft.com/azure/foundry/agents/concepts/azure-yaml-reference)),
>   mas só `dotnet run` + `curl` local foram exercitados de fato até agora. Rode os passos acima
>   você mesmo e confira a saída antes de gravar — `azd` pode pedir campos (subscription, região)
>   que não estão fixados aqui.
> - **Nome de variável pode não bater com o código.** `azure.yaml`'s `env` map declara
>   `AZURE_AI_MODEL_DEPLOYMENT_NAME`, mas `Program.cs` lê `FOUNDRY_MODEL_NAME` (com fallback pra
>   `"gpt-4o"` se não setado). Definir `AZURE_AI_MODEL_DEPLOYMENT_NAME` via `azd env set` **não**
>   chega no app como o código está escrito hoje — ou renomeia a variável em `azure.yaml` pra
>   `FOUNDRY_MODEL_NAME`, ou ajusta `Program.cs` pra ler `AZURE_AI_MODEL_DEPLOYMENT_NAME`, antes de
>   depender disso num deploy real.

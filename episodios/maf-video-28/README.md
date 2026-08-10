# maf-video-28

Sample code for a video walkthrough on **evaluating AI agents** built with the [Microsoft Agent Framework](https://github.com/microsoft/agent-framework) and Azure AI Foundry.

The repository is a small .NET 10 solution with three standalone console projects, each demonstrating a different agent evaluation technique:

| Project | What it shows |
|---|---|
| [`src/ExpectedOutputs`](src/ExpectedOutputs) | Local, LLM-free evaluation: checking an agent's responses against expected outputs (e.g. "does the answer contain the expected value?", "is the response non-empty?"). |
| [`src/EvaluationSimpleEval`](src/EvaluationSimpleEval) | Quality evaluation using Azure AI Foundry's LLM-as-judge evaluators (Relevance, Coherence) running server-side. |
| [`src/EvaluationToolCalling`](src/EvaluationToolCalling) | Evaluating an agent that uses tools/function calling, including a local check that the expected tool was actually invoked. |

## How it works

Each project creates an `AIAgent` backed by a model deployed in Azure AI Foundry (via `AIProjectClient`), runs it against a set of sample queries, and evaluates the results either with:

- **Local evaluators** (`LocalEvaluator` / `EvalChecks`) — deterministic checks that run without calling an LLM, or
- **Foundry evaluators** (`FoundryEvals`) — LLM-as-judge metrics computed by the Azure AI Foundry evaluation service.

## Requirements

- .NET 10 SDK
- An Azure AI Foundry project with a deployed chat model
- Azure CLI login (`az login`) — authentication uses `AzureCliCredential`

## Configuration

Each project reads its settings from a `.env` file (via `dotenv.net`) in the project directory:

```
FOUNDRY_PROJECT_ENDPOINT=<your Azure AI Foundry project endpoint>
FOUNDRY_MODEL=<deployed model name>   # defaults to gpt-5-mini if unset
```

## Running

```bash
dotnet run --project src/ExpectedOutputs
dotnet run --project src/EvaluationSimpleEval
dotnet run --project src/EvaluationToolCalling
```

## License

MIT — see [LICENSE](LICENSE).

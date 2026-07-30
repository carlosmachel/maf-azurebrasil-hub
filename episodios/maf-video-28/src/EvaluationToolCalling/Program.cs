using System.ComponentModel;
using Azure.AI.Projects;
using Azure.Identity;
using dotenv.net;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Foundry;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;

static string FormatMetric(EvaluationMetric metric) => metric switch
{
    NumericMetric nm => nm.Value?.ToString() ?? "null",
    BooleanMetric bm => bm.Value?.ToString() ?? "null",
    _ => metric.Interpretation?.ToString() ?? metric.Reason ?? "null",
};

[Description("Get the weather for a given location.")]
static string GetWeather([Description("The location to get the weather for.")] string location)
    => $"The weather in {location} is cloudy with a high of 15°C.";

DotEnv.Load();
string endpoint = Environment.GetEnvironmentVariable("FOUNDRY_PROJECT_ENDPOINT")!;
string deploymentName = Environment.GetEnvironmentVariable("FOUNDRY_MODEL") ?? "gpt-5-mini";

var projectClient = new AIProjectClient(new Uri(endpoint), new AzureCliCredential());

AIAgent agent = projectClient
    .AsAIAgent(
        model: deploymentName,
        instructions: "You are a travel assistant. Use the weather tool when relevant.",
        name: "TravelAgent",
        tools: [AIFunctionFactory.Create(GetWeather)]);
        
// Quando os itens contêm tool_definitions, FoundryEvals adiciona
// Tool Call Accuracy automaticamente ao conjunto padrão (Relevance/Coherence/Task Adherence).
// TaskAdherence e IntentResolution ficam de fora: o SDK (Microsoft.Agents.AI.Foundry
// 1.14.0-preview.260721.1) não inclui "tool_definitions" no data_mapping desses dois
// avaliadores, o que o serviço Foundry rejeita quando o agente tem tools. Ver
// FoundryEvalConverter.ToolEvaluators no repo microsoft/agent-framework.
//FoundryEvals evaluator = new(projectClient, deploymentName,
//    FoundryEvals.Relevance, FoundryEvals.Coherence, FoundryEvals.ToolCallAccuracy);

string[] queries = ["What's the weather like in Amsterdam?", "Should I pack an umbrella for Lisbon?"];
//AgentEvaluationResults results = await agent.EvaluateAsync(queries, evaluator);

///foreach (var item in results.Items) 
//foreach (var metric in item.Metrics)
//    Console.WriteLine($"  {metric.Key}: {FormatMetric(metric.Value)}");

//Passo 3 — checar se a ferramenta certa foi chamada (ground truth local, reaproveitando o Bloco 2):
LocalEvaluator toolCheck = new(EvalChecks.ToolCalledCheck("_Main_g_GetWeather_0_1"));

AgentEvaluationResults toolResults = await agent.EvaluateAsync(
    queries, toolCheck,
    expectedToolCalls:
    [
        [new ExpectedToolCall("_Main_g_GetWeather_0_1")],
        [new ExpectedToolCall("_Main_g_GetWeather_0_1")],
    ]);

foreach (var item in toolResults.Items)
foreach (var metric in item.Metrics)
    Console.WriteLine($"  {metric.Key}: {FormatMetric(metric.Value)}");
using Azure.AI.Projects;
using Azure.Identity;
using dotenv.net;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Foundry;

DotEnv.Load();
string endpoint = Environment.GetEnvironmentVariable("FOUNDRY_PROJECT_ENDPOINT")!;
string deploymentName = Environment.GetEnvironmentVariable("FOUNDRY_MODEL") ?? "gpt-5-mini";

AIProjectClient projectClient = new(new Uri(endpoint), new AzureCliCredential());

AIAgent agent = projectClient.AsAIAgent(
    model: deploymentName,
    instructions: "You are a helpful assistant. Provide clear, accurate answers.",
    name: "SimpleAgent");

// Evaluators de qualidade rodando no lado do Foundry (LLM-as-judge)
FoundryEvals evaluator = new(projectClient, deploymentName, FoundryEvals.Relevance, FoundryEvals.Coherence);

string[] queries = ["O que é fotosinteste?", "Como funcionam as vacinas?"];
AgentEvaluationResults results = await agent.EvaluateAsync(queries, evaluator);

Console.WriteLine($"Passed: {results.Passed}/{results.Total}");
if (results.ReportUrl is not null)
    Console.WriteLine($"Report: {results.ReportUrl}");
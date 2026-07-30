using Azure.AI.Projects;
using Azure.Identity;
using dotenv.net;
using Microsoft.Agents.AI;

DotEnv.Load();
string endpoint = Environment.GetEnvironmentVariable("FOUNDRY_PROJECT_ENDPOINT")!;
string deploymentName = Environment.GetEnvironmentVariable("FOUNDRY_MODEL") ?? "gpt-5-mini";

AIAgent agent = new AIProjectClient(new Uri(endpoint), new AzureCliCredential())
    .AsAIAgent(
        model: deploymentName,
        instructions: "You are a math tutor. Answer concisely with the numeric result.",
        name: "MathTutor");

// Checks 100% locais, sem chamada de LLM
LocalEvaluator localEvaluator = new(
    EvalChecks.ContainsExpected(),
    EvalChecks.NonEmpty());

string[] queries = ["What is 2 + 2?", "What is the square root of 144?"];
string[] expectedOutputs = ["4", "11"];

AgentEvaluationResults results = await agent.EvaluateAsync(
    queries, localEvaluator, expectedOutput: expectedOutputs);

Console.WriteLine($"Passed: {results.Passed}/{results.Total}  |  All passed: {results.AllPassed}");
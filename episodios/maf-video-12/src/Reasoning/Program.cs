using Azure.AI.OpenAI;
using Azure.Identity;
using dotenv.net;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

// A API de Responses do OpenAI ainda está em preview, então precisamos suprimir este aviso.
#pragma warning disable OPENAI001

DotEnv.Load();

string endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
                  ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set.");
string deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-5.4-mini";


// ─────────────────────────────────────────────
// 1. Registra as tools (funções C# → AIFunction)
// ─────────────────────────────────────────────
var hrTools = new Tools();

IList<AITool> tools =
[
    AIFunctionFactory.Create(hrTools.SearchEmployees),
    AIFunctionFactory.Create(hrTools.GetAverageSalaryByDepartment),
    AIFunctionFactory.Create(hrTools.GetEmployeeDetail),
    AIFunctionFactory.Create(hrTools.ListDepartments)
];

// ─────────────────────────────────────────────
// 2. Configura o cliente com reasoning ativado
// ─────────────────────────────────────────────
IChatClient client = new AzureOpenAIClient(new Uri(endpoint), 
        new DefaultAzureCredential())
    .GetResponsesClient()
    .AsIChatClient(deploymentName)
    .AsBuilder()
    .ConfigureOptions(o =>
    {
        o.Reasoning = new ReasoningOptions
        {
            Effort = ReasoningEffort.Medium,
            Output = ReasoningOutput.Full,
        };
    })
    .UseFunctionInvocation()   // Middleware que executa as tools automaticamente
    .Build();

// ─────────────────────────────────────────────
// 3. Cria o agente passando as tools
// ─────────────────────────────────────────────
string systemPrompt =
    """
    Você é um assistente de RH especializado em dados de funcionários.
    Responda sempre em português, de forma objetiva e com formatação clara.
    Use as tools disponíveis para buscar informações antes de responder.
    Nunca invente dados — se não encontrar, informe ao usuário.
    """;

AIAgent agent = new ChatClientAgent(client, tools: tools, instructions: systemPrompt);

// ─────────────────────────────────────────────
// 4. Pergunta 1 — Multi-hop: resultado de uma tool alimenta a próxima
//    O modelo precisa: pegar dados da Julia → descobrir o departamento →
//    buscar a média do departamento → comparar e dar uma OPINIÃO.
// ─────────────────────────────────────────────
Console.WriteLine("╔══════════════════════════════════════════════╗");
Console.WriteLine("║  Agente de RH com Reasoning + Tools          ║");
Console.WriteLine("╚══════════════════════════════════════════════╝");
Console.WriteLine();
Console.WriteLine("── 1. Modo não-streaming (raciocínio multi-hop) ──");
Console.WriteLine();

var response = await agent.RunAsync(
    "O salário da Julia Ramos está justo comparado com a média " +
    "do departamento dela? Ela deveria receber um aumento?");

Console.WriteLine(response.Text);
Console.WriteLine();
Console.WriteLine($"Tokens — Input: {response.Usage?.InputTokenCount}, " +
                  $"Output: {response.Usage?.OutputTokenCount}");

// ─────────────────────────────────────────────
// 5. Pergunta 2 — Estratégia: pergunta ambígua que exige planejamento
//    O modelo precisa decidir COMO abordar o problema, buscar dados
//    de múltiplos departamentos, cruzar e montar uma recomendação.
// ─────────────────────────────────────────────
Console.WriteLine();
Console.WriteLine("── 2. Modo streaming (reasoning visível) ──────");
Console.WriteLine();

await foreach (var update in agent.RunStreamingAsync(
    "Se eu precisar montar um time cross-funcional de 4 pessoas com " +
    "um representante de cada departamento, priorizando senioridade e " +
    "tempo de casa, quem você recomenda e por quê?"))
{
    foreach (var item in update.Contents)
    {
        // Raciocínio do modelo
        if (item is TextReasoningContent reasoning)
        {
            Console.Write($"\e[90m {reasoning.Text}\e[0m");
        }
        // Tool sendo chamada
        else if (item is FunctionCallContent toolCall)
        {
            Console.WriteLine();
            Console.Write($"\e[33m[TOOL CALL] {toolCall.Name}");
            Console.WriteLine($"({toolCall.Arguments})\e[0m");
        }
        // Resultado da tool
        else if (item is FunctionResultContent toolResult)
        {
            Console.Write($"\e[32m[RESULT]    {toolResult.Result}\e[0m");
            Console.WriteLine();
        }
        // Resposta final do agente
        else if (item is TextContent text)
        {
            Console.Write(text.Text);
        }
    }
}

Console.WriteLine();
Console.WriteLine();
Console.WriteLine("── Fim ─────────────────────────────────────────");
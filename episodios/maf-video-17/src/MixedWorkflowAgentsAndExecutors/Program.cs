using Azure.AI.OpenAI;
using Azure.Identity;
using dotenv.net;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using MixedWorkflowAgentsAndExecutors.Executors;

Console.WriteLine("\n=== Workflow de Code Review: Agentes + Executores ===\n");

DotEnv.Load();

var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
               ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT não configurado.");
var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-4o-mini";
var chatClient = new AzureOpenAIClient(new Uri(endpoint), 
        new DefaultAzureCredential())
    .GetChatClient(deploymentName)
    .AsIChatClient();

// --- Executores (lógica determinística) ---
CodeInputExecutor input = new();
StaticAnalysisExecutor staticAnalysis = new();
ReportToChatExecutor reportToChat = new();
ReviewSyncExecutor reviewSync = new();
FinalOutputExecutor finalOutput = new();

// --- Agentes de IA ---
AIAgent codeReviewer = new ChatClientAgent(
    chatClient,
    name: "CodeReviewer",
    instructions: @"Você é um engenheiro sênior especialista em qualidade de código.
Analise o relatório de análise estática e o código fornecido.
Identifique: problemas de legibilidade, possíveis bugs, violações de boas práticas e oportunidades de refatoração.

Responda EXATAMENTE neste formato:
SEVERIDADE: ALTA | MÉDIA | BAIXA
PROBLEMAS:
- <problema 1>
- <problema 2>
PONTOS_POSITIVOS:
- <ponto positivo>
CODIGO_ORIGINAL: <repita o trecho principal do código aqui>"
);

AIAgent summaryAgent = new ChatClientAgent(
    chatClient,
    name: "SummaryAgent",
    instructions: @"Você é um tech lead responsável por aprovar código em produção.
Com base na revisão recebida, produza um resumo executivo conciso.

Responda EXATAMENTE neste formato:
NOTA: <número de 0 a 10>
APROVADO: SIM | NÃO | COM_RESSALVAS
TOP_3_ACOES:
1. <ação prioritária 1>
2. <ação prioritária 2>
3. <ação prioritária 3>
JUSTIFICATIVA: <uma frase resumindo a decisão>"
);

// --- Montagem do workflow ---
WorkflowBuilder workflowBuilder = new WorkflowBuilder(input)
    .AddEdge(input, staticAnalysis)          // Executor: coleta métricas
    .AddEdge(staticAnalysis, reportToChat)   // Adaptador: string → ChatMessage + TurnToken
    .AddEdge(reportToChat, codeReviewer)     // Agente: revisão qualitativa
    .AddEdge(codeReviewer, reviewSync)       // Adaptador: processa saída do agente
    .AddEdge(reviewSync, summaryAgent)       // Agente: resumo executivo
    .AddEdge(summaryAgent, finalOutput)      // Executor: exibição final
    .WithOutputFrom(finalOutput);

// Código de exemplo para testar o workflow
string[] testSamples =
[
    """
    public List<int> GetEvens(List<int> nums) {
        List<int> result = new List<int>();
        for (int i = 0; i < nums.Count; i++) {
            if (nums[i] % 2 == 0) result.Add(nums[i]);
        }
        return result;
    }
    """
];

foreach (var sample in testSamples)
{
    Console.WriteLine($"\n{'=', 60}");
    Workflow workflow = workflowBuilder.Build();
    await ExecuteWorkflowAsync(workflow, sample.Trim());
    Console.WriteLine("\nPressione qualquer tecla para continuar...");
    Console.ReadKey(true);
}

Console.WriteLine("\n✅ Demo completa: Executores + AIAgents funcionando em conjunto!\n");
return;


static async Task ExecuteWorkflowAsync(Workflow workflow, string code)
{
    await using StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, code);

    await foreach (WorkflowEvent evt in run.WatchStreamAsync())
    {
        switch (evt)
        {
            case AgentResponseUpdateEvent update
                when !string.IsNullOrEmpty(update.Update.Text):
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Write(update.Update.Text);
                Console.ResetColor();
                break;

            case WorkflowErrorEvent err:
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(err.Exception?.Message ?? "Erro desconhecido.");
                Console.ResetColor();
                break;

            case ExecutorFailedEvent fail:
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"Executor '{fail.ExecutorId}' falhou: {fail.Data}");
                Console.ResetColor();
                break;
        }
    }
}
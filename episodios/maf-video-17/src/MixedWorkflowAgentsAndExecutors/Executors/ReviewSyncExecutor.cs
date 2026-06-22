using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace MixedWorkflowAgentsAndExecutors.Executors;

/// <summary>
/// Adaptador: processa a saída do CodeReviewer e aciona o SummaryAgent.
/// Extrai a severidade para logar e formata a mensagem para o próximo agente.
/// </summary>
[SendsMessage(typeof(ChatMessage))]
[SendsMessage(typeof(TurnToken))]
public sealed class ReviewSyncExecutor() : Executor<List<ChatMessage>>("ReviewSync")
{
    public override async ValueTask HandleAsync(
        List<ChatMessage> messages, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine();
        if (messages.Any(x => x.Role == ChatRole.Assistant))
        {
            string reviewText = string.Join("\n", messages.Select(m => m.Text?.Trim() ?? "")).Trim();

            // Extrai severidade da revisão para log/métricas
            string severity = "DESCONHECIDA";
            if (reviewText.Contains("SEVERIDADE: ALTA")) severity = "🔴 ALTA";
            else if (reviewText.Contains("SEVERIDADE: MÉDIA")) severity = "🟡 MÉDIA";
            else if (reviewText.Contains("SEVERIDADE: BAIXA")) severity = "🟢 BAIXA";

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"[{Id}] Revisão recebida | Severidade: {severity}");
            Console.ResetColor();

            // Passa toda a revisão para o SummaryAgent gerar o relatório executivo
            string prompt = $"""
                             Com base nesta revisão de código, produza o resumo executivo:

                             {reviewText}
                             """;

            await context.SendMessageAsync(new ChatMessage(ChatRole.User, prompt),
                cancellationToken: cancellationToken);
            await context.SendMessageAsync(new TurnToken(emitEvents: true), cancellationToken: cancellationToken);
        }
    }
}
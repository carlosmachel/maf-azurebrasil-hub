using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace MixedWorkflowAgentsAndExecutors.Executors;

/// <summary>
/// Adaptador: converte o relatório de análise estática (string) para ChatMessage + TurnToken.
/// Padrão obrigatório para conectar executores a AIAgents no Microsoft Agents Framework.
/// </summary>
[SendsMessage(typeof(ChatMessage))]
[SendsMessage(typeof(TurnToken))]
public sealed class ReportToChatExecutor(string id = "ReportToChat") : Executor<string>(id)
{
    public override async ValueTask HandleAsync(
        string report, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine($"[{Id}] Convertendo relatório estático → ChatMessage para o CodeReviewer");
        Console.ResetColor();

        // O agente receberá o relatório completo como mensagem do usuário
        await context.SendMessageAsync(new ChatMessage(ChatRole.User, report), cancellationToken: cancellationToken);
        await context.SendMessageAsync(new TurnToken(emitEvents: true), cancellationToken: cancellationToken);
    }
}
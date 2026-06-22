using Microsoft.Agents.AI.Workflows;

namespace MixedWorkflowAgentsAndExecutors.Executors;

/// <summary>
/// Recebe o código-fonte e armazena no estado do workflow para uso futuro.
/// </summary>
public sealed class CodeInputExecutor() : Executor<string, string>("CodeInput")
{
    public override async ValueTask<string> HandleAsync(
        string code, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"[{Id}] Código recebido ({code.Split('\n').Length} linhas)");
        Console.ResetColor();

        // Persiste o código original para uso pelos adaptadores
        await context.QueueStateUpdateAsync("OriginalCode", code, cancellationToken);
        return code;
    }
}
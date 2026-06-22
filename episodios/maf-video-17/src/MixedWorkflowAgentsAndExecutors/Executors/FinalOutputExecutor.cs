using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace MixedWorkflowAgentsAndExecutors.Executors;

/// <summary>
/// Exibe o relatório final formatado ao desenvolvedor.
/// </summary>
public sealed class FinalOutputExecutor() : Executor<List<ChatMessage>, string>("FinalOutput")
{
    private const string FinalOutputPrintedStateKey = "FinalOutput.Printed";

    public override async ValueTask<string> HandleAsync(
        List<ChatMessage> messages, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        string summary = string.Join("\n", messages.Select(m => m.Text ?? "")).Trim();

        bool alreadyPrinted = await context.ReadOrInitStateAsync<bool>(
            FinalOutputPrintedStateKey,
            initialStateFactory: () => false,
            cancellationToken: cancellationToken);

        if (alreadyPrinted)
        {
            return summary;
        }

        // Mark as printed for this workflow run before writing to console.
        await context.QueueStateUpdateAsync(FinalOutputPrintedStateKey, true, cancellationToken);

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n╔══════════════════════════════════════╗");
        Console.WriteLine("║       RELATÓRIO FINAL DE CODE REVIEW      ║");
        Console.WriteLine("╚══════════════════════════════════════╝");
        Console.WriteLine(summary);
        Console.WriteLine("\n[Fim do Workflow]");
        Console.ResetColor();

        return summary;
    }
}
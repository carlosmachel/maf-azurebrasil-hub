using CheckpointRehydrate.Models;
using Microsoft.Agents.AI.Workflows;

namespace CheckpointRehydrate.Executors;

/// <summary>
/// Superstep 2 — Registra o reembolso no sistema financeiro e gera comprovante.
/// </summary>
[YieldsOutput(typeof(ExpenseResult))]
internal sealed class FinanceExecutor() 
    : Executor<PolicyCheckResult>("Finance")
{
    private const string StateKey = "Finance.State";
    private ExpenseResult? _result;
    
    public override async ValueTask HandleAsync(PolicyCheckResult message, 
        IWorkflowContext context,
        CancellationToken cancellationToken = new())
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n[Finance] Processando reembolso #{message.Original.RequestId}");
        Console.ResetColor();

        await Task.Delay(600, cancellationToken); // simula integração com ERP

        var summary = message.WithinPolicy
            ? $"Reembolso integral de R$ {message.ApprovedAmount:N2} aprovado e registrado."
            : $"Reembolso parcial de R$ {message.ApprovedAmount:N2} registrado (valor solicitado: R$ {message.Original.Amount:N2}).";

        _result = new ExpenseResult(
            message.Original.RequestId,
            Approved: true,
            FinalAmount: message.ApprovedAmount,
            Summary: summary
        );

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  ✅ {summary}");
        Console.ResetColor();

        await context.YieldOutputAsync(_result, cancellationToken);
    }
    
    protected override ValueTask OnCheckpointingAsync(
        IWorkflowContext context, CancellationToken cancellation = default)
        => context.QueueStateUpdateAsync(StateKey, _result, cancellationToken: cancellation);

    protected override async ValueTask OnCheckpointRestoredAsync(
        IWorkflowContext context, CancellationToken cancellation = default)
    {
        _result = await context.ReadStateAsync<ExpenseResult>(StateKey, cancellationToken: cancellation);
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine("[Finance] ♻️  Estado restaurado do checkpoint.");
        Console.ResetColor();
    }

   
}

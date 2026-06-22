using CheckpointRehydrate.Models;
using Microsoft.Agents.AI.Workflows;

namespace CheckpointRehydrate.Executors;

/// <summary>
/// Superstep 1 — Verifica se a despesa está dentro da política da empresa.
/// Ex: limite de R$ 500 para alimentação, R$ 5.000 para viagem.
/// </summary>
[SendsMessage(typeof(PolicyCheckResult))]
internal sealed class PolicyCheckExecutor() : Executor<ExpenseRequest>("PolicyCheck")
{
    private const string StateKey = "PolicyCheck.State";
    private PolicyCheckResult? _lastResult;

    private static readonly Dictionary<string, decimal> Limits = new()
    {
        ["Alimentação"]  = 500m,
        ["Viagem"]       = 5_000m,
        ["Equipamento"]  = 3_000m,
        ["Outros"]       = 300m,
    };
    
    public override async ValueTask HandleAsync(
        ExpenseRequest message, IWorkflowContext context,
        CancellationToken cancellationToken = new())
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n[PolicyCheck] Verificando política para #{message.RequestId}");
        Console.WriteLine($"  Funcionário: {message.Employee}");
        Console.WriteLine($"  Categoria  : {message.Category}");
        Console.WriteLine($"  Valor      : R$ {message.Amount:N2}");
        Console.ResetColor();

        await Task.Delay(400, cancellationToken); // simula consulta a sistema de RH

        var limit = Limits.GetValueOrDefault(message.Category, 300m);
        var withinPolicy = message.Amount <= limit;
        var approvedAmount = withinPolicy ? message.Amount : limit;
        var note = withinPolicy
            ? $"Dentro do limite de R$ {limit:N2} para {message.Category}."
            : $"Valor excede o limite de R$ {limit:N2}. Valor aprovável: R$ {approvedAmount:N2}.";

        _lastResult = new PolicyCheckResult(message, withinPolicy, note, approvedAmount);

        Console.ForegroundColor = withinPolicy ? ConsoleColor.Green : ConsoleColor.Yellow;
        Console.WriteLine($"  Política   : {(withinPolicy ? "✅ OK" : "⚠️  Excede limite")}");
        Console.WriteLine($"  Nota       : {note}");
        Console.ResetColor();

        await context.SendMessageAsync(_lastResult, cancellationToken: cancellationToken);
    }
    
    protected override ValueTask OnCheckpointingAsync(
        IWorkflowContext context, CancellationToken cancellation = default)
        => context.QueueStateUpdateAsync(StateKey, _lastResult, cancellationToken: cancellation);

    protected override async ValueTask OnCheckpointRestoredAsync(
        IWorkflowContext context, CancellationToken cancellation = default)
    {
        _lastResult = await context.ReadStateAsync<PolicyCheckResult>(StateKey, cancellationToken: cancellation);
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine("[PolicyCheck] ♻️  Estado restaurado do checkpoint.");
        Console.ResetColor();
    }

    
}

using CheckpointResume.Models;
using Microsoft.Agents.AI.Workflows;

namespace CheckpointResume.Executors;

/// <summary>
/// Passo 2 — Calcula imposto de renda e salário líquido.
/// A alíquota é configurável em tempo de execução — isso permite que um
/// Resume do checkpoint anterior recalcule com um valor corrigido sem
/// recarregar os dados do funcionário.
/// </summary>
[SendsMessage(typeof(SalaryCalculation))]
internal sealed class CalculateSalaryExecutor(decimal taxRate) : Executor<EmployeeData>("CalculateSalary")
{
    private const string StateKey = "CalculateSalary.State";
    private SalaryCalculation? _result;

    /// <summary>Alíquota de IR. Pode ser ajustada antes de um Resume para recalcular.</summary>
    public decimal TaxRate { get; set; } = taxRate;

    public override async ValueTask HandleAsync(EmployeeData message, IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n[CalculateSalary] Calculando com alíquota {TaxRate:P0}...");
        Console.ResetColor();

        await Task.Delay(200, cancellationToken);

        var incomeTax  = message.TaxableIncome * TaxRate;
        var netSalary  = message.Source.GrossSalary - incomeTax;

        Console.WriteLine($"  💰 Salário bruto     : R$ {message.Source.GrossSalary:N2}");
        Console.WriteLine($"  🧾 Imposto ({TaxRate:P0})   : R$ {incomeTax:N2}");
        Console.WriteLine($"  ✅ Salário líquido   : R$ {netSalary:N2}");

        _result = new SalaryCalculation(message, TaxRate, incomeTax, netSalary);
        await context.SendMessageAsync(_result, cancellationToken: cancellationToken);
    }

    protected override ValueTask OnCheckpointingAsync(
        IWorkflowContext context, CancellationToken cancellation = default)
        => context.QueueStateUpdateAsync(StateKey, _result, cancellationToken: cancellation);

    protected override async ValueTask OnCheckpointRestoredAsync(
        IWorkflowContext context, CancellationToken cancellation = default)
    {
        _result = await context.ReadStateAsync<SalaryCalculation>(StateKey, cancellationToken: cancellation);
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine("[CalculateSalary] ♻️  Estado restaurado do checkpoint.");
        Console.ResetColor();
    }
}
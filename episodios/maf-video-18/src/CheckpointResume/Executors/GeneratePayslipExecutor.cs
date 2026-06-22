using CheckpointResume.Models;
using Microsoft.Agents.AI.Workflows;

namespace CheckpointResume.Executors;

/// <summary>
/// Passo 3 — Emite o holerite com todos os dados calculados.
/// </summary>
[YieldsOutput(typeof(Payslip))]
internal sealed class GeneratePayslipExecutor() : Executor<SalaryCalculation>("GeneratePayslip")
{
    private const string StateKey = "GeneratePayslip.State";
    private Payslip? _result;

    public override async ValueTask HandleAsync(SalaryCalculation message, IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n[GeneratePayslip] Emitindo holerite...");
        Console.ResetColor();

        await Task.Delay(150, cancellationToken);

        var src = message.Employee.Source;
        _result = new Payslip(src.EmployeeId, src.Name, src.Period,
            src.GrossSalary, message.TaxRate, message.IncomeTax, message.NetSalary,
            message.Employee.Benefits);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\n╔══════════════════════════════════════════╗");
        Console.WriteLine($"║          HOLERITE EMITIDO                ║");
        Console.WriteLine($"╚══════════════════════════════════════════╝");
        Console.WriteLine($"  Funcionário : {_result.Name} ({_result.EmployeeId})");
        Console.WriteLine($"  Período     : {_result.Period}");
        Console.WriteLine($"  Bruto       : R$ {_result.GrossSalary:N2}");
        Console.WriteLine($"  IR ({_result.TaxRate:P0})    : R$ {_result.IncomeTax:N2}");
        Console.WriteLine($"  Líquido     : R$ {_result.NetSalary:N2}");
        Console.ResetColor();

        await context.YieldOutputAsync(_result, cancellationToken);
    }

    protected override ValueTask OnCheckpointingAsync(
        IWorkflowContext context, CancellationToken cancellation = default)
        => context.QueueStateUpdateAsync(StateKey, _result, cancellationToken: cancellation);

    protected override async ValueTask OnCheckpointRestoredAsync(
        IWorkflowContext context, CancellationToken cancellation = default)
    {
        _result = await context.ReadStateAsync<Payslip>(StateKey, cancellationToken: cancellation);
    }
}

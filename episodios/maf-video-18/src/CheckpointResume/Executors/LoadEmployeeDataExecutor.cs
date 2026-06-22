using CheckpointResume.Models;
using Microsoft.Agents.AI.Workflows;

namespace CheckpointResume.Executors;

/// <summary>
/// Passo 1 — Carrega dados do funcionário: renda tributável e benefícios.
/// Operação potencialmente cara (consulta ERP/RH). Salva checkpoint para
/// que um re-cálculo posterior não precise repetir esse passo.
/// </summary>
[SendsMessage(typeof(EmployeeData))]
internal sealed class LoadEmployeeDataExecutor() : Executor<PayrollRequest>("LoadEmployeeData")
{
    private const string StateKey = "LoadEmployeeData.State";
    private EmployeeData? _result;

    private static readonly Dictionary<string, List<string>> BenefitsByDept = new()
    {
        ["Engenharia"] = ["Vale Alimentação R$ 800", "Plano de Saúde", "Home-Office"],
        ["Vendas"]     = ["Comissão variável", "Vale Alimentação R$ 600", "Plano de Saúde"],
        ["Financeiro"] = ["Vale Alimentação R$ 700", "Plano de Saúde", "PLR"],
    };

    public override async ValueTask HandleAsync(PayrollRequest message, IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n[LoadEmployeeData] Carregando dados de {message.Name} ({message.Department})...");
        Console.ResetColor();

        await Task.Delay(500, cancellationToken); // simula consulta ao ERP/RH

        var benefits    = BenefitsByDept.GetValueOrDefault(message.Department, ["Vale Alimentação R$ 500"]);
        var taxableIncome = message.GrossSalary * 0.85m; // 15% de deduções padrão

        Console.WriteLine($"  📋 Renda tributável : R$ {taxableIncome:N2}");
        Console.WriteLine($"  🎁 Benefícios       : {string.Join(", ", benefits)}");

        _result = new EmployeeData(message, taxableIncome, benefits);
        await context.SendMessageAsync(_result, cancellationToken: cancellationToken);
    }

    protected override ValueTask OnCheckpointingAsync(
        IWorkflowContext context, CancellationToken cancellation = default)
        => context.QueueStateUpdateAsync(StateKey, _result, cancellationToken: cancellation);

    protected override async ValueTask OnCheckpointRestoredAsync(
        IWorkflowContext context, CancellationToken cancellation = default)
    {
        _result = await context.ReadStateAsync<EmployeeData>(StateKey, cancellationToken: cancellation);
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine("[LoadEmployeeData] ♻️  Estado restaurado do checkpoint (consulta ERP ignorada).");
        Console.ResetColor();
    }
}

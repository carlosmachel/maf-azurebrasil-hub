// ═══════════════════════════════════════════════════════════════════════
//  Cenário: folha de pagamento com recálculo de IR via Resume.
//
//  Demonstra: Resume = workflow concluído com sucesso → restore de
//             checkpoint → re-execução na MESMA instância de run.
//
//  Caso real:
//    RH fechou a folha de Maio com alíquota de IR 15% (incorreta).
//    Após auditoria, a alíquota correta é 22%.
//    Com Resume, os dados do funcionário (passo caro / ERP) já estão
//    no Checkpoint #1 — não são recarregados. Só CalculateSalary e
//    GeneratePayslip re-executam com a alíquota corrigida.
//
//  FASE 1: Folha gerada com alíquota 15% (errada).
//  FASE 2: Resume do Checkpoint #1 com alíquota corrigida 22%.
// ═══════════════════════════════════════════════════════════════════════

using CheckpointResume.Executors;
using CheckpointResume.Models;
using Microsoft.Agents.AI.Workflows;

Print("══════════════════════════════════════════════════════");
Print("  Checkpoint e Resume");
Print("  Cenário: Recálculo de Folha de Pagamento");
Print("══════════════════════════════════════════════════════");

var request = new PayrollRequest(
    EmployeeId:  "EMP-1042",
    Name:        "Fernanda Oliveira Costa",
    Department:  "Engenharia",
    GrossSalary: 12_500m,
    Period:      "Maio/2026"
);

// ── Executors ─────────────────────────────────────────────────────────
var loadEmployeeDataExecutor = new LoadEmployeeDataExecutor();
var calculateSalaryExecutor  = new CalculateSalaryExecutor(taxRate: 0.15m); // alíquota inicial (errada)
var generatePayslipExecutor  = new GeneratePayslipExecutor();

var checkpointManager = CheckpointManager.CreateInMemory();
var checkpoints = new List<CheckpointInfo>();

var workflow = BuildWorkflow();

// ── FASE 1 — Execução completa com alíquota incorreta ─────────────────
Print("\n▶ FASE 1 — Gerando folha com alíquota 15% (incorreta)...\n", ConsoleColor.DarkCyan);

StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, request, checkpointManager);

await foreach (var evt in run.WatchStreamAsync())
{
    switch (evt)
    {
        case ExecutorCompletedEvent ec:
            Print($"  * Executor {ec.ExecutorId} concluído.", ConsoleColor.DarkGray);
            break;

        case SuperStepCompletedEvent ss:
        {
            var cp = ss.CompletionInfo?.Checkpoint;
            if (cp is not null)
            {
                checkpoints.Add(cp);
                Print($"  💾 Checkpoint #{checkpoints.Count} salvo — {cp.CheckpointId[..8]}...", ConsoleColor.DarkGreen);
            }
            break;
        }

        case WorkflowOutputEvent:
            Print("\n  ✅ Folha gerada com alíquota 15%.", ConsoleColor.Green);
            break;

        case WorkflowErrorEvent err:
            Print($"\n  ❌ Erro: {err.Exception?.Message}", ConsoleColor.Red);
            break;
    }
}

// ── FASE 2 — RH corrige alíquota + Resume do Checkpoint #1 ────────────
//
//  Resume opera na MESMA instância de run após conclusão com sucesso.
//  Checkpoint #1 tem o estado de LoadEmployeeData salvo — esse executor
//  NÃO re-executa (OnCheckpointRestoredAsync restaura o estado do ERP).
//  Apenas CalculateSalary e GeneratePayslip re-executam com nova alíquota.

Print("\n▶ FASE 2 — RH corrigiu alíquota: 15% → 22%. Recalculando...\n", ConsoleColor.DarkCyan);

if (checkpoints.Count >= 1)
{
    // Ajusta a alíquota no executor — válido para a próxima execução
    calculateSalaryExecutor.TaxRate = 0.22m;

    // Restaura para o Checkpoint #1 (pós-LoadEmployeeData) e re-executa
    await run.RestoreCheckpointAsync(checkpoints[0], CancellationToken.None);

    await foreach (var evt in run.WatchStreamAsync())
    {
        switch (evt)
        {
            case ExecutorCompletedEvent ec:
                Print($"  * Executor {ec.ExecutorId} concluído.", ConsoleColor.DarkGray);
                break;

            case SuperStepCompletedEvent ss:
            {
                var cp = ss.CompletionInfo?.Checkpoint;
                if (cp is not null)
                {
                    checkpoints.Add(cp);
                    Print($"  💾 Checkpoint #{checkpoints.Count} salvo — {cp.CheckpointId[..8]}...", ConsoleColor.DarkGreen);
                }
                break;
            }

            case WorkflowOutputEvent:
                Print("\n  ✅ Holerite corrigido emitido (alíquota 22%).", ConsoleColor.Green);
                break;

            case WorkflowErrorEvent err:
                Print($"\n  ❌ Erro: {err.Exception?.Message}", ConsoleColor.Red);
                break;
        }
    }

    Print("\n  💡 Ponto-chave:", ConsoleColor.White);
    Print("     LoadEmployeeData NÃO re-executou — consulta ao ERP veio do Checkpoint #1.");
    Print("     CalculateSalary + GeneratePayslip re-executaram com alíquota corrigida.");
    Print("\n  Resume vs Rehydrate:", ConsoleColor.White);
    Print("     Resume    → mesma instância, run JÁ CONCLUÍDO → recalcular/replay.");
    Print("     Rehydrate → nova instância                    → recovery após falha/crash.");
}

await run.DisposeAsync();
Print("\n══════════════════════════════════════════════════════\n");
return;

Workflow BuildWorkflow() =>
    new WorkflowBuilder(loadEmployeeDataExecutor)
        .AddEdge(loadEmployeeDataExecutor, calculateSalaryExecutor)
        .AddEdge(calculateSalaryExecutor, generatePayslipExecutor)
        .Build();

static void Print(string msg, ConsoleColor color = ConsoleColor.Gray)
{
    Console.ForegroundColor = color;
    Console.WriteLine(msg);
    Console.ResetColor();
}

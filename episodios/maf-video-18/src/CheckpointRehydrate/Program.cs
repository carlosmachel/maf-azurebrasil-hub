// ═══════════════════════════════════════════════════════════════════════
//  Cenário: reembolso de despesa de funcionário.
//  Demonstra: salvar checkpoints + criar NOVA instância de workflow
//             que retoma o estado salvo (cross-process / pós-restart).
// ═══════════════════════════════════════════════════════════════════════

using CheckpointRehydrate.Executors;
using CheckpointRehydrate.Models;
using Microsoft.Agents.AI.Workflows;

Print("══════════════════════════════════════════════════════");
Print("  Checkpoint e Reidratação");
Print("  Cenário: Reembolso de Despesa de Funcionário");
Print("══════════════════════════════════════════════════════");

// ── Pedido de reembolso ───────────────────────────────────────────────
var request = new ExpenseRequest(
    RequestId:   "EXP-2025-0089",
    Employee:    "Carlos Eduardo Martins",
    Category:    "Viagem",
    Amount:      3_800m,
    Description: "Passagem + hospedagem — visita ao cliente em SP"
);

// ── Workflow ──────────────────────────────────────────────────────────
//  ExpenseRequest → [PolicyCheck] → PolicyCheckResult → [Finance] → ExpenseResult

var policyExecutor = new PolicyCheckExecutor();
var financeExecutor = new FinanceExecutor();

// ── EXECUÇÃO ORIGINAL com checkpoint ─────────────────────────────────
Print("\n▶ FASE 1 — Executando workflow original...\n", ConsoleColor.DarkCyan);

var checkpointManager = CheckpointManager.CreateInMemory();
var checkpoints = new List<CheckpointInfo>();
var workflow = BuildWorkflow();

var run = await InProcessExecution
    .RunStreamingAsync(workflow, request, checkpointManager);

await foreach (var evt in run.WatchStreamAsync())
{
    if (evt is SuperStepCompletedEvent ss)
    {
        var cp = ss.CompletionInfo?.Checkpoint;
        if (cp is not null)
        {
            checkpoints.Add(cp);
            Print($"  💾 Checkpoint #{checkpoints.Count} salvo — {cp.CheckpointId[..8]}...",
                ConsoleColor.DarkGreen);
        }
    }
    if (evt is WorkflowOutputEvent)
        Print("\n  ✅ Workflow original concluído.\n", ConsoleColor.Green);
}

// ── REIDRATAÇÃO — nova instância, mesmo estado ────────────────────────
//
//  Imagine que o servidor reiniciou após o Checkpoint 1.
//  O workflow não precisa recomeçar do zero: basta carregar
//  o checkpoint e criar uma nova instância com InProcessExecution.ResumeStreamingAsync.

if (checkpoints.Count >= 1)
{
    Print("▶ FASE 2 — Simulando reinício de servidor...", ConsoleColor.DarkCyan);
    Print("  Reidratando workflow a partir do Checkpoint 1 em nova instância...\n",
        ConsoleColor.DarkCyan);

    // Nova instância — sem memória do que já aconteceu
    var freshWorkflow = BuildWorkflow();

    var rehydratedRun = await InProcessExecution
        .ResumeStreamingAsync(freshWorkflow, checkpoints[0], checkpointManager);

    await foreach (var evt in rehydratedRun.WatchStreamAsync())
    {
        if (evt is WorkflowOutputEvent)
        {
            Print("\n  ✅ Reidratação concluída — nova instância retomou exatamente do ponto salvo.",
                ConsoleColor.Green);
        }
    }

    Print("\n  💡 Ponto-chave:", ConsoleColor.White);
    Print("     Resume  → mesma instância de run (mesmo processo, retry local)");
    Print("     Rehydrate → nova instância        (cross-process, pós-restart, migração)");
}

Print("\n══════════════════════════════════════════════════════\n");
return;

Workflow BuildWorkflow() =>
    new WorkflowBuilder(policyExecutor)
        .AddEdge(policyExecutor, financeExecutor)
        .Build();

static void Print(string msg, ConsoleColor color = ConsoleColor.Gray)
{
    Console.ForegroundColor = color;
    Console.WriteLine(msg);
    Console.ResetColor();
}

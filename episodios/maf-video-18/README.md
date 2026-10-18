# Ep. 18 — Workflow checkpoints: Resume vs. Rehydrate

> Part of the **Microsoft Agent Framework** video series on [AzureBrasil.Cloud](https://azurebrasil.cloud) (PT-BR). All episodes: [maf-azurebrasil-hub](https://carlosmachel.github.io/maf-azurebrasil-hub/)

Two business scenarios that show how `Microsoft.Agents.AI.Workflows` checkpoints let a workflow continue from a saved state instead of starting from scratch. **No LLM or Azure resources required.**

## Projects

### `CheckpointResume` — payroll recalculation

```
PayrollRequest → [LoadEmployeeData] → [CalculateSalary] → [GeneratePayslip]
                        💾 #1               💾 #2
```

HR closed May's payroll with the wrong income-tax rate (15%). After the audit the correct rate is 22%. With **Resume**, the run restores checkpoint #1: the expensive ERP load is **not** executed again, only `CalculateSalary` and `GeneratePayslip` re-run with the fixed rate.

### `CheckpointRehydrate` — expense reimbursement

```
ExpenseRequest → [PolicyCheck] → [Finance] → ExpenseResult
```

Simulates a server restart: a **new workflow instance** is created and `InProcessExecution.ResumeStreamingAsync` continues from the saved checkpoint.

## Key takeaway

| | Resume | Rehydrate |
|---|---|---|
| Instance | same run | new instance |
| Typical use | local retry, fix-and-rerun | cross-process, after restart, migration |

## Run

```bash
dotnet run --project src/CheckpointResume
dotnet run --project src/CheckpointRehydrate
```

Checkpoints use `CheckpointManager.CreateInMemory()`; replace it with a persistent store for production.

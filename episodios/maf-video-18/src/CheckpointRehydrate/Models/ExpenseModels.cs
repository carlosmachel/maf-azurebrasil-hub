namespace CheckpointRehydrate.Models;

// Pedido de reembolso de despesa
public record ExpenseRequest(
    string RequestId,
    string Employee,
    string Category,       // Viagem, Alimentação, Equipamento, etc.
    decimal Amount,
    string Description
);

// Após análise de política
public record PolicyCheckResult(
    ExpenseRequest Original,
    bool WithinPolicy,
    string PolicyNote,
    decimal ApprovedAmount
);

// Resultado final
public record ExpenseResult(
    string RequestId,
    bool Approved,
    decimal FinalAmount,
    string Summary
);
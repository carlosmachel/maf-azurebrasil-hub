namespace Reasoning;

/// <summary>
/// Representa um funcionário na base de dados.
/// </summary>
public record Employee(
    int      Id,
    string   Name,
    string   Department,
    string   Role,
    decimal  Salary,
    DateTime HireDate
);
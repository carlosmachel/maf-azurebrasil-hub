namespace CheckpointResume.Models;

// Pedido de processamento de folha
public record PayrollRequest(
    string EmployeeId,
    string Name,
    string Department,
    decimal GrossSalary,
    string Period
);

// Dados carregados do funcionário (passo 1)
public record EmployeeData(
    PayrollRequest Source,
    decimal TaxableIncome,
    List<string> Benefits
);

// Resultado do cálculo salarial (passo 2)
public record SalaryCalculation(
    EmployeeData Employee,
    decimal TaxRate,
    decimal IncomeTax,
    decimal NetSalary
);

// Holerite final (passo 3 — output)
public record Payslip(
    string EmployeeId,
    string Name,
    string Period,
    decimal GrossSalary,
    decimal TaxRate,
    decimal IncomeTax,
    decimal NetSalary,
    List<string> Benefits
);
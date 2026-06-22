// HRTools.cs — Tools de RH que o agente pode chamar
// Cada método público vira uma AIFunction disponível para o modelo.

using System.ComponentModel;
using System.Text.Json;
using Reasoning;

/// <summary>
/// Conjunto de tools de RH. O modelo decide sozinho quais chamar
/// com base no raciocínio sobre a pergunta do usuário.
/// </summary>
public class Tools
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    // ─────────────────────────────────────────────
    // Tool 1 — Buscar funcionários com filtros
    // ─────────────────────────────────────────────
    [Description("Busca funcionários na base de dados. Permite filtrar por departamento " +
                 "e/ou tempo mínimo de empresa em anos. Retorna JSON com a lista.")]
    public string SearchEmployees(
        [Description("Departamento para filtrar (ex: 'Engineering', 'Product'). " +
                     "Null ou vazio para buscar todos.")] string? department,
        [Description("Tempo mínimo de empresa em anos completos. " +
                     "Null para não filtrar por tempo.")] int? minTenureYears)
    {
        var query = FakeDb.Employees.AsQueryable();

        if (!string.IsNullOrWhiteSpace(department))
            query = query.Where(e =>
                e.Department.Equals(department, StringComparison.OrdinalIgnoreCase));

        if (minTenureYears.HasValue)
            query = query.Where(e =>
                (DateTime.Now - e.HireDate).TotalDays / 365.0 >= minTenureYears.Value);

        var results = query.Select(e => new
        {
            e.Id,
            e.Name,
            e.Department,
            e.Role,
            HireDate      = e.HireDate.ToString("yyyy-MM-dd"),
            YearsAtCompany = Math.Floor((DateTime.Now - e.HireDate).TotalDays / 365)
        }).ToList();

        return JsonSerializer.Serialize(results, JsonOptions);
    }

    // ─────────────────────────────────────────────
    // Tool 2 — Salário médio por departamento
    // ─────────────────────────────────────────────
    [Description("Retorna o salário médio (em R$) de um departamento específico, " +
                 "junto com o número de funcionários no cálculo.")]
    public string GetAverageSalaryByDepartment(
        [Description("Nome exato do departamento (ex: 'Engineering', 'Product', 'Design')."
        )] string department)
    {
        var employees = FakeDb.Employees
            .Where(e => e.Department.Equals(department, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (!employees.Any())
            return JsonSerializer.Serialize(new { error = $"Departamento '{department}' não encontrado." }, JsonOptions);

        var avg = employees.Average(e => e.Salary);

        return JsonSerializer.Serialize(new
        {
            department,
            headcount   = employees.Count,
            avgSalary   = Math.Round(avg, 2),
            minSalary   = employees.Min(e => e.Salary),
            maxSalary   = employees.Max(e => e.Salary),
        }, JsonOptions);
    }

    // ─────────────────────────────────────────────
    // Tool 3 — Perfil completo de um funcionário
    // ─────────────────────────────────────────────
    [Description("Retorna o perfil completo de um funcionário pelo ID, incluindo " +
                 "cargo, salário e data de contratação.")]
    public string GetEmployeeDetail(
        [Description("ID numérico do funcionário (obtido via SearchEmployees).")] int employeeId)
    {
        var employee = FakeDb.Employees.FirstOrDefault(e => e.Id == employeeId);

        if (employee is null)
            return JsonSerializer.Serialize(new { error = $"Funcionário com ID {employeeId} não encontrado." }, JsonOptions);

        return JsonSerializer.Serialize(new
        {
            employee.Id,
            employee.Name,
            employee.Department,
            employee.Role,
            employee.Salary,
            HireDate       = employee.HireDate.ToString("yyyy-MM-dd"),
            YearsAtCompany = Math.Floor((DateTime.Now - employee.HireDate).TotalDays / 365),
        }, JsonOptions);
    }

    // ─────────────────────────────────────────────
    // Tool 4 — Listar departamentos disponíveis
    // ─────────────────────────────────────────────
    [Description("Lista todos os departamentos cadastrados na base de dados " +
                 "com a contagem de funcionários em cada um.")]
    public string ListDepartments()
    {
        var departments = FakeDb.Employees
            .GroupBy(e => e.Department)
            .Select(g => new { department = g.Key, headcount = g.Count() })
            .OrderBy(d => d.department)
            .ToList();

        return JsonSerializer.Serialize(departments, JsonOptions);
    }
}
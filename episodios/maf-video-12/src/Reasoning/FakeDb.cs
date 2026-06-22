namespace Reasoning;

/// <summary>
/// Base de dados em memória para fins didáticos.
/// Simula uma tabela de funcionários com dados realistas.
/// </summary>
public static class FakeDb
{
    public static readonly List<Employee> Employees =
    [
        new(1, "Ana Lima",       "Engineering", "Tech Lead",           12_000m, new DateTime(2019,  3, 10)),
        new(2, "Carlos Mota",    "Engineering", "Senior Engineer",     14_500m, new DateTime(2020,  7, 22)),
        new(3, "Julia Ramos",    "Engineering", "Junior Engineer",     11_000m, new DateTime(2023,  1,  5)),
        new(4, "Pedro Souza",    "Product",     "Product Manager",     13_000m, new DateTime(2018, 11,  3)),
        new(5, "Mariana Costa",  "Design",      "UX Designer",         10_500m, new DateTime(2021,  6, 18)),
        new(6, "Rafael Torres",  "Product",     "Product Analyst",      9_800m, new DateTime(2022,  9, 14)),
        new(7, "Fernanda Silva", "Engineering", "Mid-level Engineer",  12_800m, new DateTime(2021,  4, 27)),
        new(8, "Lucas Pereira",  "Design",      "Product Designer",    11_200m, new DateTime(2019,  8,  1)),
        new(9, "Beatriz Nunes",  "HR",          "HR Business Partner",  9_500m, new DateTime(2020,  2, 15)),
        new(10,"Diego Alves",    "HR",          "Recruiter",            8_200m, new DateTime(2023,  5, 30)),
    ];
}
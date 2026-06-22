using Microsoft.Agents.AI.Workflows;

namespace MixedWorkflowAgentsAndExecutors.Executors;

/// <summary>
/// Realiza análise estática do código: contagem de linhas, detecção de linguagem,
/// identificação de padrões problemáticos (SQL concatenado, dynamic, etc.).
/// Este executor é 100% determinístico — sem IA.
/// </summary>
public sealed class StaticAnalysisExecutor() : Executor<string, string>("StaticAnalysis")
{
    public override ValueTask<string> HandleAsync(
        string code, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var lines = code.Split('\n');
        int totalLines = lines.Length;
        int blankLines = lines.Count(l => string.IsNullOrWhiteSpace(l));
        int codeLines = totalLines - blankLines;

        // Detecção de padrões problemáticos (análise léxica simples)
        var warnings = new List<string>();

        if (code.Contains("dynamic "))
            warnings.Add("⚠ Uso de 'dynamic' detectado — perde segurança de tipos");
        if (code.Contains("SELECT *"))
            warnings.Add("⚠ 'SELECT *' detectado — preferir colunas explícitas");
        if (code.Contains("+ u.") || code.Contains("+ id") || code.Contains("WHERE id="))
            warnings.Add("⚠ Possível SQL injection por concatenação de string");
        if (code.Contains("Password=") || code.Contains("password="))
            warnings.Add("🔴 Credencial hardcoded detectada — risco de segurança crítico");
        if (!code.Contains("//") && !code.Contains("///"))
            warnings.Add("⚠ Ausência de comentários/documentação XML");
        if (code.Contains("for (int i"))
            warnings.Add("ℹ Laço for clássico — considere LINQ ou foreach");

        // Detecção simples de linguagem
        string language = code.Contains("public ") || code.Contains("private ") ? "C#" : "Desconhecida";

        // Estimativa de complexidade ciclomática (contagem de branches)
        int complexity = 1;
        complexity += code.Split(new[] { "if ", "else if", "for ", "foreach ", "while ", "catch ", "case " },
            StringSplitOptions.None).Length - 1;

        string report = $"""
                         === RELATÓRIO DE ANÁLISE ESTÁTICA ===
                         Linguagem detectada : {language}
                         Total de linhas     : {totalLines} ({codeLines} de código, {blankLines} em branco)
                         Complexidade estimada: {complexity}
                         Avisos encontrados  : {warnings.Count}
                         {(warnings.Any() ? string.Join("\n", warnings.Select(w => $"  {w}")) : "  Nenhum aviso automático")}

                         === CÓDIGO PARA REVISÃO ===
                         {code}
                         """;

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[{Id}] Análise estática concluída: {warnings.Count} aviso(s), complexidade {complexity}");
        Console.ResetColor();

        return ValueTask.FromResult(report);
    }
}
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Azure.AI.OpenAI;
using Azure.Identity;
using dotenv.net;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

DotEnv.Load();

// ---------------------------------------------------------------------------
// Configuração OpenTelemetry
// ---------------------------------------------------------------------------

const string sourceName = "OpenTelemetryDemo";
const string serviceName = "AgentOpenTelemetry";

var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")
    ?? "http://localhost:4317";

// Traces
using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName, serviceVersion: "1.0.0"))
    .AddSource(sourceName)
    .AddHttpClientInstrumentation()
    .AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint))
    .Build();

// Metrics
using var meterProvider = Sdk.CreateMeterProviderBuilder()
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName, serviceVersion: "1.0.0"))
    .AddMeter(sourceName)
    .AddHttpClientInstrumentation()
    .AddRuntimeInstrumentation()
    .AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint))
    .Build();

// Logging estruturado
var services = new ServiceCollection();
services.AddLogging(b => b
    .SetMinimumLevel(LogLevel.Debug)
    .AddOpenTelemetry(o =>
    {
        o.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName, serviceVersion: "1.0.0"));
        o.AddOtlpExporter(oo => oo.Endpoint = new Uri(otlpEndpoint));
        o.IncludeScopes = true;
        o.IncludeFormattedMessage = true;
    }));

using var activitySource = new ActivitySource(sourceName);
using var meter = new Meter(sourceName);

var interactionCounter = meter.CreateCounter<int>(
    "agent.interactions.total",
    description: "Total de interações com o agente");

var responseTimeHistogram = meter.CreateHistogram<double>(
    "agent.response.duration",
    unit: "s",
    description: "Duração das respostas do agente em segundos");

var toolCallCounter = meter.CreateCounter<int>(
    "agent.tool.calls.total",
    description: "Total de chamadas a ferramentas pelo agente");

// ---------------------------------------------------------------------------
// Ferramentas (tools) disponíveis para o agente
// ---------------------------------------------------------------------------

[Description("Retorna a previsão do tempo para uma cidade.")]
static async Task<string> GetWeatherAsync(
    [Description("Nome da cidade")] string city)
{
    await Task.Delay(2000);
    return $"O tempo em {city} está nublado com máxima de 22°C e ventos de 15 km/h.";
}

[Description("Converte um valor de uma moeda para outra.")]
static async Task<string> ConvertCurrencyAsync(
    [Description("Valor a converter")] double amount,
    [Description("Moeda de origem, ex: USD")] string from,
    [Description("Moeda de destino, ex: BRL")] string to)
{
    await Task.Delay(1000);
    var rates = new Dictionary<string, double>
    {
        ["USD_BRL"] = 5.10, ["BRL_USD"] = 0.196,
        ["EUR_BRL"] = 5.55, ["BRL_EUR"] = 0.180,
        ["USD_EUR"] = 0.92, ["EUR_USD"] = 1.09
    };
    var key = $"{from.ToUpper()}_{to.ToUpper()}";
    if (rates.TryGetValue(key, out var rate))
        return $"{amount} {from.ToUpper()} = {amount * rate:F2} {to.ToUpper()} (taxa: {rate})";

    return $"Conversão de {from} para {to} não disponível no momento.";
}

// ---------------------------------------------------------------------------
// Criação do agente
// ---------------------------------------------------------------------------
var serviceProvider = services.BuildServiceProvider();
var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
var logger = loggerFactory.CreateLogger<Program>();

var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
    ?? throw new InvalidOperationException("Variável AZURE_OPENAI_ENDPOINT não definida.");
var deployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-4o-mini";

logger.LogInformation("Iniciando AgentOpenTelemetry com deployment {Deployment}", deployment);

using var instrumentedChatClient = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
    .GetChatClient(deployment)
    .AsIChatClient()
    .AsBuilder()
    .UseFunctionInvocation()
     .UseOpenTelemetry(sourceName: sourceName, configure: cfg => cfg.EnableSensitiveData = true)
    .Build();

var agent = new ChatClientAgent(
        instrumentedChatClient,
        name: "DemoAgent",
        instructions: """
            Você é um assistente prestativo que responde em português.
            Seja conciso. Quando precisar de dados externos, use as ferramentas disponíveis.
            """,
        tools:
        [
            AIFunctionFactory.Create(GetWeatherAsync),
            AIFunctionFactory.Create(ConvertCurrencyAsync),
        ])
    .AsBuilder()
    .UseOpenTelemetry(sourceName: sourceName, configure: cfg => cfg.EnableSensitiveData = true)
    .Build();

var session = await agent.CreateSessionAsync();

logger.LogInformation("Agente criado. ID: {AgentId}", agent.Id);

// ---------------------------------------------------------------------------
// Loop de interação
// ---------------------------------------------------------------------------

Console.WriteLine("""
    ╔══════════════════════════════════════════════════════════╗
    ║        Demo OpenTelemetry — Microsoft Agent Framework    ║
    ╠══════════════════════════════════════════════════════════╣
    ║  Dashboard: http://localhost:4318                        ║
    ║  Ferramentas: clima e câmbio (verifique os traces!)      ║
    ║  Digite 'exit' para sair.                                ║
    ╚══════════════════════════════════════════════════════════╝

    Sugestões de mensagens para o demo:
      → "Qual é o tempo em São Paulo?"
      → "Converta 100 dólares para reais"
      → "Como está o tempo em Londres e quanto custa 50 euros em reais?"
    """);

using var sessionActivity = activitySource.StartActivity("AgentSession");
var sessionId = Guid.NewGuid().ToString("N")[..8];

sessionActivity?
    .SetTag("session.id", sessionId)
    .SetTag("agent.name", "DemoAgent")
    .SetTag("session.start", DateTimeOffset.UtcNow.ToString("O"));

Console.WriteLine($"TraceId da sessão: {sessionActivity?.TraceId}\n");

using (logger.BeginScope(new Dictionary<string, object>
{
    ["SessionId"] = sessionId,
    ["AgentName"] = "DemoAgent"
}))
{
    var interactionCount = 0;

    while (true)
    {
        Console.Write("Você: ");
        var input = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(input) || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
            break;

        interactionCount++;

        using var interactionActivity = activitySource.StartActivity("AgentInteraction");
        interactionActivity?
            .SetTag("interaction.number", interactionCount)
            .SetTag("user.input", input)
            .SetTag("agent.name", "DemoAgent");

        logger.LogInformation(
            "Interação #{Number} iniciada. Input: {Input}",
            interactionCount, input);

        var sw = Stopwatch.StartNew();

        try
        {
            Console.Write("\nAgente: ");

            await foreach (var update in agent.RunStreamingAsync(input, session))
            {
                Console.Write(update.Text);
            }

            Console.WriteLine("\n");

            sw.Stop();

            interactionCounter.Add(1,
                new KeyValuePair<string, object?>("status", "success"),
                new KeyValuePair<string, object?>("agent", "DemoAgent"));

            responseTimeHistogram.Record(sw.Elapsed.TotalSeconds,
                new KeyValuePair<string, object?>("status", "success"));

            interactionActivity?
                .SetTag("response.success", true)
                .SetTag("response.duration_s", sw.Elapsed.TotalSeconds);

            logger.LogInformation(
                "Interação #{Number} concluída em {Duration:F2}s",
                interactionCount, sw.Elapsed.TotalSeconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            Console.WriteLine($"\n[Erro: {ex.Message}]\n");

            interactionCounter.Add(1,
                new KeyValuePair<string, object?>("status", "error"),
                new KeyValuePair<string, object?>("agent", "DemoAgent"));

            responseTimeHistogram.Record(sw.Elapsed.TotalSeconds,
                new KeyValuePair<string, object?>("status", "error"));

            interactionActivity?
                .SetTag("response.success", false)
                .SetTag("error.type", ex.GetType().Name)
                .SetStatus(ActivityStatusCode.Error, ex.Message);

            logger.LogError(ex,
                "Interação #{Number} falhou após {Duration:F2}s",
                interactionCount, sw.Elapsed.TotalSeconds);
        }
    }

    sessionActivity?
        .SetTag("session.total_interactions", interactionCount)
        .SetTag("session.end", DateTimeOffset.UtcNow.ToString("O"));

    logger.LogInformation(
        "Sessão encerrada. Total de interações: {Total}",
        interactionCount);
}

Console.WriteLine("Encerrando. Os dados ainda aparecem no Aspire por alguns segundos.");

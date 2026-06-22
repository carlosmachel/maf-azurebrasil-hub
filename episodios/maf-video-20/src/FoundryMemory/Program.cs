// LinkedIn Post Agent with Foundry Memory
// Demo: o agente aprende o estilo de escrita do usuário em 2 posts
// e aplica na terceira solicitação — mesmo em sessão nova.

using System.Text.Json;
using Azure.AI.Projects;
using Azure.Identity;

using dotenv.net;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Foundry;
using Microsoft.Extensions.AI;

#pragma warning disable MAAI001

DotEnv.Load();

var foundryEndpoint    = Environment.GetEnvironmentVariable("AZURE_AI_PROJECT_ENDPOINT")       ?? throw new InvalidOperationException("AZURE_AI_PROJECT_ENDPOINT não definido.");
var memoryStoreName    = Environment.GetEnvironmentVariable("AZURE_AI_MEMORY_STORE_ID")        ?? "linkedin-memory-store-video";
var deploymentName     = Environment.GetEnvironmentVariable("AZURE_AI_MODEL_DEPLOYMENT_NAME")  ?? "gpt-5.4-mini";
var embeddingModelName = Environment.GetEnvironmentVariable("AZURE_AI_EMBEDDING_DEPLOYMENT_NAME") ?? "text-embedding-3-large";

// AzureCliCredential garante que o token do `az login` é usado diretamente,
// sem ser sobrescrito por variáveis de ambiente (AZURE_CLIENT_ID, etc.).
AzureCliCredential credential = new();
AIProjectClient projectClient = new(new Uri(foundryEndpoint), credential);

// Escopo fixo por usuário: o estilo aprendido persiste entre sessões.
FoundryMemoryProvider memoryProvider = new(
    projectClient,
    memoryStoreName,
    stateInitializer: _ => new FoundryMemoryProvider.State(new FoundryMemoryProviderScope("linkedin-user-002")));

var options = new ChatClientAgentOptions()
    {
        Name = "LinkedInStyleAgent",
        ChatOptions = new ChatOptions(),
        AIContextProviders = [memoryProvider]
    };
options.ChatOptions.ModelId = deploymentName;
options.ChatOptions.Instructions = """
                                       Você é um especialista em criação de posts para LinkedIn.
                                       Analise os posts que o usuário compartilhar e memorize o estilo dele:
                                       tamanho, tom, uso de listas, abertura, encerramento e linguagem.
                                       Quando pedido para criar um novo post, reproduza fielmente esse padrão —
                                       nunca invente um estilo diferente do aprendido.
                                   """;
ChatClientAgent agent = projectClient.AsAIAgent(
    options);

await memoryProvider.EnsureMemoryStoreCreatedAsync(
    deploymentName,
    embeddingModelName,
    "Armazena o padrão de escrita do usuário para posts no LinkedIn");

// ── SESSÃO 1: aprendizado ──────────────────────────────────────────────────

AgentSession session1 = await agent.CreateSessionAsync();

Console.WriteLine("=== SESSÃO 1 — Aprendizado de estilo ===\n");

// Post de exemplo 1
string post1 = """
    A maioria das empresas de IA ainda não entende o básico.

    Não é sobre o modelo.
    É sobre o contexto que você dá a ele.

    Modelo ruim + contexto rico → resultado útil.
    Modelo excelente + contexto vazio → lixo.

    Para de trocar de LLM toda semana.
    Começa a pensar em dados.

    #IA #Produto #Engenharia
""";

Console.WriteLine(await agent.RunAsync(
    $"Aqui está um post meu para você aprender meu estilo:\n\n{post1}", session1));

// Post de exemplo 2
string post2 = """
    Passei 3 anos tentando automatizar o que deveria ter delegado.

    Automação resolve escala.
    Delegação resolve julgamento.

    São problemas diferentes.

    Se o seu processo exige decisão humana, automatizar vai só esconder o problema.

    Entenda primeiro. Automatize depois.

    #Gestão #Liderança #Processos
""";

Console.WriteLine(await agent.RunAsync(
    $"Mais um post meu — continue aprendendo meu padrão:\n\n{post2}", session1));

Console.WriteLine("\nAguardando o Foundry Memory processar os posts...");
await memoryProvider.WhenUpdatesCompletedAsync();
Console.WriteLine("Memória atualizada.\n");

// ── SESSÃO 2: geração com estilo aprendido (wow moment) ───────────────────

Console.WriteLine("=== SESSÃO 2 — Nova sessão, estilo preservado ===\n");

AgentSession session2 = await agent.CreateSessionAsync();

Console.WriteLine(await agent.RunAsync(
    "Crie um post sobre o uso de memória em agentes de IA. Use exatamente o meu estilo.", session2));

Console.WriteLine("\nAguardando atualização de memória...");
await memoryProvider.WhenUpdatesCompletedAsync();

// ── SESSÃO 3: refinamento ──────────────────────────────────────────────────

Console.WriteLine("\n=== SESSÃO 3 — Refinamento em follow-up ===\n");

AgentSession session3 = await agent.CreateSessionAsync();

Console.WriteLine(await agent.RunAsync(
    "Preciso de um post sobre por que a maioria dos projetos de IA falha antes de chegar em produção. Meu estilo.", session3));

Console.WriteLine(await agent.RunAsync(
    "Está bom. Deixa mais provocador na abertura — como se fosse um soco no estômago.", session3));

Console.WriteLine("\n✓ Demo concluída.");

using Azure.AI.OpenAI;
using Azure.Identity;
using dotenv.net;
using Microsoft.Agents.AI;
using OpenAI.Chat;

DotEnv.Load();

// --- Configuration ---
string endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
                  ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set.");
string deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-4.1-mini";

// --- Skills Provider ---
#pragma warning disable MAAI001
var skillsProvider = new FileAgentSkillsProvider(skillPath: Path.Combine(AppContext.BaseDirectory, "skills"));
#pragma warning restore MAAI001

// --- Agent Setup ---
AIAgent agent = new AzureOpenAIClient(new Uri(endpoint), 
        new DefaultAzureCredential())
    .GetChatClient(deploymentName)
    .AsAIAgent(new ChatClientAgentOptions()
    {
        Name = "SkillsAgent",
        ChatOptions = new()
        {
            Instructions = """
                You are a helpful corporate assistant at Contoso.
                When a skill provides an asset (template, checklist, form), you MUST use it as
                the basis of your answer — render it in full, fill in any data the user provided,
                and highlight what is still missing.
                Always prefer structured formats (tables, checklists) from the skill assets over
                free-form text.
                """,
        },
        AIContextProviders = [skillsProvider]
    });

// --- Example 1: Expense policy question (loads FAQ resource) ---
Console.WriteLine("Example 1: Checking expense policy FAQ");
Console.WriteLine("---------------------------------------");
AgentResponse response1 = await agent.RunAsync("Are tips reimbursable? I left a 25% tip on a taxi ride and want to know if that's covered.");
Console.WriteLine($"Agent: {response1.Text}\n");


// --- Example 2: Filing an expense report (multi-turn with template asset) ---
Console.WriteLine("Example 2: Filing an expense report");
Console.WriteLine("---------------------------------------");
AgentSession session1 = await agent.CreateSessionAsync();
AgentResponse response2 = await agent.RunAsync(
    "I need to file an expense report. Please use the expense report template to create a draft with these expenses from last week: 3 client dinners ($180 each at Olive Garden with 4 attendees: me, João, Maria, and the client Pedro — business purpose: Q3 deal review) and a $1,200 flight from NYC to SF on United Airlines. Fill in what you can and flag anything that's missing or needs approval.",
    session1);
Console.WriteLine($"Agent: {response2.Text}\n");
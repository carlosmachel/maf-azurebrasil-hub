// Sales report generator: the agent calls a function to fetch monthly revenue data,
// then uses Code Interpreter to generate a chart and a professional PDF report.

#pragma warning disable OPENAI001

using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Responses;
using System.ComponentModel;
using System.Text.Json;
using dotenv.net;

const string instructions = """
You are an automated financial analyst with access to Code Interpreter. Your task is to generate professional financial reports.

When asked for a report, you MUST:
1. Call get_monthly_revenue to fetch monthly data
2. Use Code Interpreter to generate visualizations:
   - Create a bar chart with matplotlib for monthly revenue
   - Create a CSV file with the data
   - Create a professional PDF report using reportlab that includes the chart
3. Save all files with descriptive names
4. In your response, mention the specific file names you created

You must always use Code Interpreter for this task. Do not skip this step.
""";



// --- Fake data simulating a database call ---
[Description("Returns monthly revenue data for the current year.")]
static string GetMonthlyRevenue()
{
    var data = new[]
    {
        new { month = "Jan", revenue = 124500 },
        new { month = "Fev", revenue = 98700  },
        new { month = "Mar", revenue = 137200 },
        new { month = "Abr", revenue = 152800 },
        new { month = "Mai", revenue = 143600 },
        new { month = "Jun", revenue = 168900 },
        new { month = "Jul", revenue = 155400 },
        new { month = "Ago", revenue = 172100 },
        new { month = "Set", revenue = 161300 },
        new { month = "Out", revenue = 189700 },
        new { month = "Nov", revenue = 204500 },
        new { month = "Dez", revenue = 231800 },
    };
    return JsonSerializer.Serialize(data);
}

DotEnv.Load();

var endpoint  = Environment.GetEnvironmentVariable("AZURE_AI_PROJECT_ENDPOINT")      ?? throw new InvalidOperationException("AZURE_AI_PROJECT_ENDPOINT is not set.");
var deploymentName = Environment.GetEnvironmentVariable("AZURE_AI_MODEL_DEPLOYMENT_NAME") ?? "gpt-4o-mini";

AIProjectClient aiProjectClient = 
    new(new Uri(endpoint), new DefaultAzureCredential());

AIAgent agent = aiProjectClient.AsAIAgent(
    deploymentName,
    instructions: instructions,
    name: "RevenueReportAgent",
    tools:
    [
        new HostedCodeInterpreterTool(),
        AIFunctionFactory.Create(GetMonthlyRevenue)
    ]);

AgentSession session = await agent.CreateSessionAsync();

Console.WriteLine("=== Revenue Report Agent ===\n");

string prompt = """
Generate a financial report with a chart for this year's revenue.
IMPORTANT: You MUST use the code interpreter to:
1. Call get_monthly_revenue to fetch the data
2. Generate a bar chart using matplotlib
3. Create a PDF report using reportlab with the chart embedded
4. Save the files and provide the filenames
""";

AgentResponse response = await agent.RunAsync(prompt, session);

// Display the text response
Console.WriteLine("📋 Agent Response:\n");
foreach (TextContent textContent in response.Messages.SelectMany(x => x.Contents).OfType<TextContent>())
{
    Console.WriteLine(textContent.Text);
}

// Debug: Show all content types in response
Console.WriteLine("\n🔍 Response Contents Debug:");
int messageCount = 0;
foreach (var message in response.Messages)
{
    messageCount++;
    Console.WriteLine($"\n  📨 Message #{messageCount} - Type: {message.GetType().Name}");
    
    if (message.Contents == null || message.Contents.Count == 0)
    {
        Console.WriteLine($"     (No contents in this message)");
        continue;
    }

    foreach (var content in message.Contents)
    {
        Console.WriteLine($"     - Content Type: {content.GetType().Name}");
        
        if (content.Annotations == null)
        {
            Console.WriteLine($"       (No annotations)");
        }
        else
        {
            Console.WriteLine($"       Annotations count: {content.Annotations.Count}");
            foreach (var annotation in content.Annotations)
            {
                Console.WriteLine($"         - Annotation: {annotation.GetType().Name}");
                if (annotation is CitationAnnotation cite)
                {
                    Console.WriteLine($"           Raw Type: {cite.RawRepresentation?.GetType().Name}");
                    Console.WriteLine($"           Raw Value: {cite.RawRepresentation}");
                }
            }
        }
    }
}

Console.WriteLine($"\n📊 Total messages: {messageCount}");


// Extract container file citations from response annotations and download.
// AIProjectClient.GetProjectOpenAIClient() returns a ProjectOpenAIClient (inherits from OpenAI.OpenAIClient)
// which supports GetContainerClient(), unlike AzureOpenAIClient which does not.
var containerClient = aiProjectClient
    .GetProjectOpenAIClient()
    .GetContainerClient();

HashSet<string> downloadedFiles = [];
bool foundContainerFiles = false;

foreach (AIContent content in response.Messages.SelectMany(x => x.Contents))
{
    if (content.Annotations is null)
    {
        continue;
    }

    foreach (AIAnnotation annotation in content.Annotations)
    {
        // Container files from Code Interpreter have ContainerFileCitationMessageAnnotation as raw representation
        if (annotation is CitationAnnotation citation
            && citation.RawRepresentation is ContainerFileCitationMessageAnnotation containerCitation)
        {
            foundContainerFiles = true;

            // Deduplicate by container+file ID in case the same file is cited multiple times
            string key = $"{containerCitation.ContainerId}/{containerCitation.FileId}";
            if (!downloadedFiles.Add(key))
            {
                continue;
            }

            Console.WriteLine($"\n📥 Downloading container file: {containerCitation.Filename}");
            Console.WriteLine($"  Container ID: {containerCitation.ContainerId}");
            Console.WriteLine($"  File ID:      {containerCitation.FileId}");

            BinaryData fileData = await containerClient
                .DownloadContainerFileAsync(
                containerCitation.ContainerId,
                containerCitation.FileId);

            // Sanitize filename to prevent path traversal
            string safeFilename = Path.GetFileName(containerCitation.Filename);
            string outputPath = Path.Combine(Directory.GetCurrentDirectory(), safeFilename);
            await File.WriteAllBytesAsync(outputPath, fileData.ToArray());
            Console.WriteLine($"  ✅ Saved to:  {outputPath}");
        }
    }
}

if (!foundContainerFiles)
{
    Console.WriteLine("\n⚠️  No container file citations found in the response.");
    Console.WriteLine("The model may not have generated a downloadable file for this prompt.");
}


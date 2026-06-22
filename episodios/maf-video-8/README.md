# Microsoft Agent Framework - DevUI Demo

This repository contains the source code for the eighth video in the Microsoft Agent Framework playlist on the AzureBrasil.Cloud channel.

## 📖 Overview

This project demonstrates the **DevUI** (Developer UI) feature of the Microsoft Agent Framework, a powerful debugging and testing interface for AI agents. DevUI provides an interactive web-based interface to test and debug your AI agents in real-time during development.

## 🎯 What is DevUI?

DevUI is a built-in developer interface included in the Microsoft Agent Framework that allows you to:

- **Interact with AI agents** in real-time through a web interface
- **Test agent behaviors** and responses without building a custom UI
- **Debug workflows** and multi-agent interactions
- **Inspect tool calls** and function executions
- **Accelerate development** by providing immediate feedback during the development process

DevUI is automatically available in Development mode and provides a quick way to validate your agent configurations, tools, and workflows.

## 🛠️ Technologies & Tools

### Framework & Runtime
- **.NET 10.0** - Latest .NET framework
- **ASP.NET Core** - Web application framework

### Microsoft Agent Framework Libraries

| Package | Version | Purpose |
|---------|---------|---------|
| `Microsoft.Agents.AI` | `1.0.0-rc2` | Core agent framework functionality |
| `Microsoft.Agents.AI.DevUI` | `1.0.0-preview.260225.1` | Developer UI interface for testing and debugging agents |
| `Azure.AI.OpenAI` | `2.8.0-beta.1` | Azure OpenAI integration |
| `Azure.Identity` | `1.18.0` | Azure authentication and identity management |
| `Microsoft.AspNetCore.OpenApi` | `10.0.0` | OpenAPI support |

## 🚀 Features Demonstrated

This project showcases:

### 1. Multiple AI Agents
- **Assistant** - A helpful assistant with weather and time tools
- **Poet** - A creative poet that responds in verse
- **Coder** - An expert programmer with calculation tools

### 2. AI Tools (Functions)
- `get_weather` - Get weather information for a location
- `get_current_time` - Get the current time
- `add` - Calculate the sum of two numbers

### 3. Agent Workflows
- **Review Workflow** - A sequential workflow that chains an assistant agent with a reviewer agent

### 4. DevUI Integration
The project demonstrates how to enable and use DevUI for:
- Testing individual agents
- Testing agent workflows
- Debugging tool/function calls
- Inspecting agent responses

## 📋 Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- An Azure OpenAI resource with a deployed model (e.g., GPT-4o-mini)
- Azure CLI or appropriate Azure credentials configured

## ⚙️ Configuration

1. Update the `appsettings.json` file with your Azure OpenAI configuration:

```json
{
  "Settings": {
    "Uri": "https://your-azure-endpoint.openai.azure.com/",
    "DeploymentName": "gpt-4o-mini"
  }
}
```

2. Ensure you have proper Azure authentication set up. The application uses `DefaultAzureCredential`, which supports:
   - Azure CLI authentication
   - Managed Identity
   - Environment variables
   - And other Azure authentication methods

## 🏃 Running the Project

1. Navigate to the Api project directory:
```bash
cd src/Api
```

2. Restore dependencies:
```bash
dotnet restore
```

3. Run the application:
```bash
dotnet run
```

4. The application will start and display:
```
DevUI is available at: https://localhost:7067/devui
Press Ctrl+C to stop the server.
```

5. Open your browser and navigate to: **https://localhost:7067/devui**

## 🎮 Using DevUI

Once the application is running and you access DevUI:

1. **Select an Agent** - Choose from the available agents (assistant, poet, coder, or review-workflow)
2. **Start a Conversation** - Type messages and interact with the agent
3. **Test Tools** - Ask the agent to use its available tools (e.g., "What's the weather in São Paulo?")
4. **Inspect Responses** - View detailed information about agent responses and tool calls
5. **Debug Workflows** - Test multi-agent workflows and see how agents interact

## 📁 Project Structure

```
maf-video-8/
├── src/
│   └── Api/
│       ├── Program.cs          # Main application setup and agent configuration
│       ├── Settings.cs         # Configuration settings model
│       ├── Api.csproj          # Project dependencies
│       ├── appsettings.json    # Application configuration
│       └── Properties/
│           └── launchSettings.json
├── README.md
└── LICENSE
```

## 📚 Key Code Highlights

### Registering Agents with Tools
```csharp
builder.AddAIAgent("assistant", "You are a helpful assistant.")
    .WithAITools(
        AIFunctionFactory.Create(GetWeather, name: "get_weather"),
        AIFunctionFactory.Create(GetCurrentTime, name: "get_current_time")
    );
```

### Creating Workflows
```csharp
builder.AddWorkflow("review-workflow", (sp, key) =>
{
    var agents = new List<IHostedAgentBuilder>() { assistantBuilder, reviewerBuilder }
        .Select(ab => sp.GetRequiredKeyedService<AIAgent>(ab.Name));
    return AgentWorkflowBuilder.BuildSequential(workflowName: key, agents: agents);
}).AddAsAIAgent();
```

### Enabling DevUI
```csharp
if (builder.Environment.IsDevelopment())
{
    app.MapDevUI();
}
```

## 🎥 Video Tutorial

This project is part of the Microsoft Agent Framework video series on the AzureBrasil.Cloud YouTube channel. Check out the video for a complete walkthrough and explanation.

## 📝 License

This project is licensed under the terms specified in the LICENSE file.

## 🔗 Resources

- [Microsoft Agent Framework Documentation](https://learn.microsoft.com/en-us/azure/ai-services/agents/)
- [Azure OpenAI Service](https://azure.microsoft.com/en-us/products/ai-services/openai-service)
- [.NET Documentation](https://learn.microsoft.com/en-us/dotnet/)

## 🤝 Contributing

Feel free to fork this repository and experiment with different agent configurations, tools, and workflows!

---

**Note**: DevUI is only available in Development mode. For production deployments, you should build a custom UI or use the API endpoints directly.

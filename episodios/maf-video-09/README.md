# MAF Video 09 - Microsoft Agent Framework with Model Context Protocol

This repository contains the code for the ninth video in the Microsoft Agent Framework (MAF) playlist on the [AzureBrasil.Cloud](https://azurebrasil.cloud) channel.

## Project Overview

This project demonstrates the integration of **Microsoft Agent Framework** with the **Model Context Protocol (MCP)** in .NET. It showcases how to build AI agents that can communicate with MCP servers to extend their capabilities and access external tools.

## Project Structure

The solution consists of three main projects:

### 1. **AgentWithMcp** 🤖
An AI agent powered by Azure OpenAI that communicates with MCP servers. This project demonstrates:
- Integration with Azure OpenAI services
- HTTP client transport for MCP communication
- Session management and state persistence
- RESTful API endpoints for agent interactions
- OpenAPI/Scalar UI for API documentation

**Key Endpoints:**
- `GET /microsoft-learn` - Query the agent with a user input and optional session state

### 2. **McpServer** 🔧
A Model Context Protocol server that exposes tools via HTTP transport. This project includes:
- MCP server setup with HTTP transport
- Built-in tools exposed as MCP resources
- Tool registration and discovery

**Available Tools:**
- `Echo` - Echoes the message back to the client
- `ReverseEcho` - Returns the message reversed

### 3. **AsMcpTool** 🛠️
A sample implementation demonstrating how to expose services as MCP tools. This project shows patterns for:
- Defining custom MCP tools
- Tool configuration and registration
- Integration with other systems

## Technology Stack

- **.NET 10.0** - Latest .NET runtime
- **Azure OpenAI** - AI capabilities
- **Model Context Protocol (MCP)** - Tool integration standard
- **ASP.NET Core** - Web framework
- **Azure Identity** - Authentication
- **Scalar** - API documentation UI

## Getting Started

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download) or later
- Azure OpenAI account and credentials
- Visual Studio, Visual Studio Code, or Rider

### Configuration

1. **Update `appsettings.json`** in the `AgentWithMcp` project:
   ```json
   {
     "Settings": {
       "Uri": "your-azure-openai-uri",
       "DeploymentName": "your-deployment-name"
     }
   }
   ```

2. **Update `appsettings.Development.json`** if needed for local development settings.

### Running the Projects

#### Option 1: Run all projects
```bash
cd src/McpServer
dotnet run &

cd ../AsMcpTool
dotnet run &

cd ../AgentWithMcp
dotnet run
```

#### Option 2: Using the solution file
```bash
dotnet build maf-video-09.sln
dotnet run --project src/McpServer/McpServer.csproj &
dotnet run --project src/AsMcpTool/AsMcpTool.csproj &
dotnet run --project src/AgentWithMcp/AgentWithMcp.csproj
```

### Testing the API

Once the application is running:

1. Open your browser and navigate to `https://localhost:5001` (or the configured HTTPS port)
2. You'll see the Scalar API documentation interface
3. Try the `/microsoft-learn` endpoint with a test query

**Example request:**
```
GET /microsoft-learn?userInput=What%20is%20the%20Model%20Context%20Protocol%3F
```

## How It Works

1. **AgentWithMcp** receives a user query through the REST API
2. The agent creates an HTTP client transport to connect to the **McpServer**
3. The agent retrieves available tools from the MCP server
4. Azure OpenAI processes the query and decides which tools to use
5. The agent invokes the appropriate tools (Echo, ReverseEcho, etc.)
6. Results are returned to the user

## Key Features

✅ **Agent Framework Integration** - Demonstrates modern AI agent patterns  
✅ **MCP Support** - Uses the Model Context Protocol for tool integration  
✅ **HTTP Transport** - MCP communication over HTTP  
✅ **State Management** - Session persistence and state serialization  
✅ **OpenAPI Documentation** - Scalar UI for easy API exploration  
✅ **Azure OpenAI Integration** - Powered by Azure's AI services  
✅ **Extensible Architecture** - Easy to add new tools and capabilities

## Development

### Building the Solution
```bash
dotnet build maf-video-09.sln
```

### Running Tests
```bash
dotnet test maf-video-09.sln
```

### Publishing
```bash
dotnet publish -c Release
```

## Learning Resources

This project is based on:
- [Microsoft Agent Framework Documentation](https://learn.microsoft.com/en-us/dotnet/api/microsoft.agents.ai)
- [Model Context Protocol Specification](https://spec.modelcontextprotocol.io/)
- [Azure OpenAI Documentation](https://learn.microsoft.com/en-us/azure/ai-services/openai/)

## Video Reference

This code accompanies the 9th video in the Microsoft Agent Framework series on [AzureBrasil.Cloud](https://azurebrasil.cloud).

## License

This project is licensed under the [LICENSE](./LICENSE) file included in this repository.

## Contributing

Contributions are welcome! Feel free to open issues or submit pull requests to improve this project.

## Support

For questions or issues, please open an issue in this repository or reach out to the community on the AzureBrasil.Cloud channel.

---

**Happy coding! 🚀**

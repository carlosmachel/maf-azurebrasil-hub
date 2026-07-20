using Azure.AI.AgentServer.Invocations;
using HostedInvocationsEchoAgent;

var builder = AgentHost.CreateBuilder(args);

// Register the echo agent as a singleton (no LLM needed).
builder.Services.AddSingleton<EchoAiAgent>();

// Register the Invocations SDK services and wire the handler.
builder.Services.AddInvocationsServer();
builder.Services.AddScoped<InvocationHandler, EchoInvocationHandler>();

// Map the Invocations protocol endpoints:
//   POST /invocations              — invoke the agent
//   GET  /invocations/{id}         — get result (not used by this sample)
//   POST /invocations/{id}/cancel  — cancel (not used by this sample)
// AgentHost also wires up GET /readiness, which the platform polls to know
// the container is ready to serve traffic.
builder.RegisterProtocol("invocations", endpoints => endpoints.MapInvocationsServer());

var app = builder.Build();

app.Run();
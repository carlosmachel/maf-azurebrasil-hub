# Ep. 13 — Observabilidade com OpenTelemetry

Parte da série [Microsoft Agent Framework](https://www.youtube.com/playlist?list=PLagWy4RVE5D-zG5tHWz5f_Vt8Mp6p1F--) · índice de todos os episódios no [maf-azurebrasil-hub](https://github.com/carlosmachel/maf-azurebrasil-hub).

Um agente de console (`ChatClientAgent`) com duas tools — clima e câmbio — instrumentado com OpenTelemetry. Traces, métricas e logs vão via OTLP para o **Aspire Dashboard**, onde dá para ver cada chamada ao modelo, cada tool e o tempo de cada etapa.

## O que o episódio mostra

- `.UseOpenTelemetry()` no `IChatClient` **e** no agente (convenções `gen_ai.*`)
- `ActivitySource` próprio para spans de sessão e de interação
- Métricas customizadas: `agent.interactions.total`, `agent.response.duration`, `agent.tool.calls.total`
- Logs estruturados com escopo (`SessionId`, `AgentName`) exportados via OTLP
- `EnableSensitiveData = true` para ver prompts e respostas no trace (use só em dev)

```
Console App (C#)
├── ChatClientAgent (Microsoft.Agents.AI)
│   └── Azure OpenAI (IChatClient)
│       ├── Tool: GetWeatherAsync
│       └── Tool: ConvertCurrencyAsync
└── OpenTelemetry SDK
    ├── TracerProvider  → OTLP → Aspire Dashboard
    ├── MeterProvider   → OTLP → Aspire Dashboard
    └── LoggerProvider  → OTLP → Aspire Dashboard
```

## Pré-requisitos

- .NET 10 SDK
- Docker
- Um deployment no Azure OpenAI / Microsoft Foundry e `az login` (a autenticação usa `DefaultAzureCredential`, sem chave de API)

## Como rodar

1. Suba o Aspire Dashboard:

   ```bash
   docker run -d --name aspire-dashboard \
     -p 4318:18888 -p 4317:18889 \
     -e DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true \
     mcr.microsoft.com/dotnet/aspire-dashboard:latest
   ```

   (ou `docker build -f Dockerfile.aspire -t aspire-dashboard .`)

2. Crie `src/Observability/.env`:

   ```env
   AZURE_OPENAI_ENDPOINT=https://<seu-recurso>.openai.azure.com/
   AZURE_OPENAI_DEPLOYMENT_NAME=gpt-4o-mini
   # opcional
   OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
   ```

3. Rode:

   ```bash
   dotnet run --project src/Observability
   ```

4. Abra http://localhost:4318 e converse com o agente. Experimente:
   - "Qual é o tempo em São Paulo?"
   - "Converta 100 dólares para reais"
   - "Como está o tempo em Londres e quanto custa 50 euros em reais?"

## Licença

MIT

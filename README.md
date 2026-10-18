# Microsoft Agent Framework — Series Hub

[![Build Episodes](https://github.com/carlosmachel/maf-azurebrasil-hub/actions/workflows/build-episodes.yml/badge.svg)](https://github.com/carlosmachel/maf-azurebrasil-hub/actions/workflows/build-episodes.yml)

Code for every episode of the **Microsoft Agent Framework** series on [AzureBrasil.Cloud](https://azurebrasil.cloud) (videos in PT-BR, code in C# / .NET 10).

▶️ Playlists: [Microsoft Agent Framework](https://www.youtube.com/playlist?list=PLagWy4RVE5D-zG5tHWz5f_Vt8Mp6p1F--) · [Microsoft Foundry](https://www.youtube.com/playlist?list=PLagWy4RVE5D9gpd0t82q6PHEVTuNNArbi)
🌐 GitHub Pages: https://carlosmachel.github.io/maf-azurebrasil-hub/

Each episode lives in its own `maf-video-NN` repository. Every week a GitHub Action copies them into [`episodios/`](episodios) and another one builds all of them with .NET 10. Episodes 1–7 are in [maf-playlist](https://github.com/carlosmachel/maf-playlist).

<!-- EPISODES:START -->
## 🎬 Episódios

- **Ep. 08 — [Microsoft Agent Framework - DevUI Demo](https://github.com/carlosmachel/maf-video-8)** — This repository contains the source code for the eighth video in the Microsoft Agent Framework playlist on the AzureBrasil.Cloud channel.
- **Ep. 09 — [MAF Video 09 - Microsoft Agent Framework with Model Context Protocol](https://github.com/carlosmachel/maf-video-09)** — This repository contains the code for the ninth video in the Microsoft Agent Framework (MAF) playlist on the [AzureBrasil.Cloud](https://azurebrasil.cloud) channel.
- **Ep. 10 — [Ep. 10 — Long-term memory with `ChatHistoryMemoryProvider`](https://github.com/carlosmachel/maf-video-10)** — > Part of the **Microsoft Agent Framework** video series on [AzureBrasil.Cloud](https://azurebrasil.cloud) (PT-BR). All episodes: [maf-azurebrasil-hub](https://carlosmachel.github.io/maf-azurebrasil-hub/)
- **Ep. 11 — [Agent Skills — Microsoft.Agents.AI](https://github.com/carlosmachel/maf-video-11)** — Example of using **`FileAgentSkillsProvider`** from the [`Microsoft.Agents.AI`](https://www.nuget.org/packages/Microsoft.Agents.AI) package to give an AI agent "skills" via Markdown files.
- **Ep. 12 — [Ep. 12 — Reasoning models + tools (multi-hop)](https://github.com/carlosmachel/maf-video-12)** — > Part of the **Microsoft Agent Framework** video series on [AzureBrasil.Cloud](https://azurebrasil.cloud) (PT-BR). All episodes: [maf-azurebrasil-hub](https://carlosmachel.github.io/maf-azurebrasil-hub/)
- **Ep. 13 — [maf-video-13](https://github.com/carlosmachel/maf-video-13)** — Repositório para o vídeo 13 da série do Microsoft Agent Framework
- **Ep. 14 — [🧠 Compaction Pipeline — Conceitos (Agent Framework)](https://github.com/carlosmachel/maf-video-14)** — O **Compaction Pipeline** é uma estratégia para reduzir o histórico de conversas enviado para o LLM, mantendo o contexto essencial enquanto controla:
- **Ep. 15 — [maf-video-15](https://github.com/carlosmachel/maf-video-15)** — Confira o repositório do episódio.
- **Ep. 16 — [maf-video-16](https://github.com/carlosmachel/maf-video-16)** — Repository for the MAF video series - Ep 16
- **Ep. 17 — [Mixed Workflow — Agents + Executors](https://github.com/carlosmachel/maf-video-17)** — Example of mixing **AI agents** and **custom executors** in a single workflow using the [`Microsoft.Agents.AI.Workflows`](https://www.nuget.org/packages/Microsoft.Agents.AI.Workflows) package.
- **Ep. 18 — [Ep. 18 — Workflow checkpoints: Resume vs. Rehydrate](https://github.com/carlosmachel/maf-video-18)** — > Part of the **Microsoft Agent Framework** video series on [AzureBrasil.Cloud](https://azurebrasil.cloud) (PT-BR). All episodes: [maf-azurebrasil-hub](https://carlosmachel.github.io/maf-azurebrasil-hub/)
- **Ep. 19 — [Ep. 19 — Code Interpreter with Microsoft Foundry](https://github.com/carlosmachel/maf-video-19)** — > Part of the **Microsoft Agent Framework** video series on [AzureBrasil.Cloud](https://azurebrasil.cloud) (PT-BR). All episodes: [maf-azurebrasil-hub](https://carlosmachel.github.io/maf-azurebrasil-hub/)
- **Ep. 20 — [maf-video-20 — Microsoft Foundry Memory](https://github.com/carlosmachel/maf-video-20)** — Demonstrates how an AI agent can learn a user's writing style from example LinkedIn posts and reproduce it faithfully — even across completely new sessions — using **Microsoft Foundry Memory**.
- **Ep. 21 — [maf-video-21 — Vibe Coding Research Agent](https://github.com/carlosmachel/maf-video-21)** — A console-based AI agent built on **Microsoft Agents AI** and **Azure AI Foundry** that researches AI developer tools and the vibe coding ecosystem. The agent streams responses, supports plan/execute modes, persists file memory across sessions, and can fetch and convert web pages to Markdown on demand.
- **Ep. 22 — [ToolbBoxTools — Code Review Agent with Azure AI Foundry Toolbox via MCP](https://github.com/carlosmachel/maf-video-22)** — A .NET 10 console application that demonstrates how to build an AI code review agent using **Azure AI Foundry**, **Microsoft Agents SDK**, and **Model Context Protocol (MCP)**. The agent connects to a Foundry Toolbox backed by the Roslyn Analyzers repository (via GitMCP), then generates a structured markdown report for a given C# file.
- **Ep. 23 — [Ep. 23 — Harness Agent with background sub-agents](https://github.com/carlosmachel/maf-video-23)** — > Part of the **Microsoft Agent Framework** video series on [AzureBrasil.Cloud](https://azurebrasil.cloud) (PT-BR). All episodes: [maf-azurebrasil-hub](https://carlosmachel.github.io/maf-azurebrasil-hub/)
- **Ep. 24 — [maf-video-24](https://github.com/carlosmachel/maf-video-24)** — Episódio 24 - Structured Output in Foundry
- **Ep. 25 — [Permission-Aware RAG with SQL Server VECTOR + Microsoft Agent Framework](https://github.com/carlosmachel/maf-video-25)** — This demo shows how to build a RAG agent whose retrieval is filtered by per-user file permissions. Each user only receives answers grounded in documents they have access to — enforced at the SQL query level using the native `VECTOR` type in SQL Server 2025, with an optional in-memory caching layer that can be invalidated on demand.
- **Ep. 26 — [maf-video-26](https://github.com/carlosmachel/maf-video-26)** — Confira o repositório do episódio.
- **Ep. 27 — [Ep. 27 — Foundry Hosted Agents (.NET)](https://github.com/carlosmachel/maf-video-27)** — > Part of the **Microsoft Agent Framework** video series on [AzureBrasil.Cloud](https://azurebrasil.cloud) (PT-BR). All episodes: [maf-azurebrasil-hub](https://carlosmachel.github.io/maf-azurebrasil-hub/)
- **Ep. 28 — [maf-video-28](https://github.com/carlosmachel/maf-video-28)** — Sample code for a video walkthrough on **evaluating AI agents** built with the [Microsoft Agent Framework](https://github.com/microsoft/agent-framework) and Azure AI Foundry.
- **Ep. 29 — [maf-video-29](https://github.com/carlosmachel/maf-video-29)** — Confira o repositório do episódio.
- **Ep. 30 — [maf-video-30](https://github.com/carlosmachel/maf-video-30)** — Confira o repositório do episódio.
<!-- EPISODES:END -->

# Modular Batteries

A cross-platform desktop AI workspace built with C#, Avalonia, Mojo, and local model tooling such as OpenCode and Ollama.

<div align="center">

![C#](https://img.shields.io/badge/C%23-239120?logo=csharp&logoColor=white)
![.NET](https://img.shields.io/badge/.NET-512BD4?logo=dotnet&logoColor=white)
![Avalonia](https://img.shields.io/badge/Avalonia-5C2D91?logo=avalonia&logoColor=white)
![Mojo](https://img.shields.io/badge/Mojo-FF7A00?logo=mojo&logoColor=white)
![Python](https://img.shields.io/badge/Python-3776AB?logo=python&logoColor=white)
![FastAPI](https://img.shields.io/badge/FastAPI-009688?logo=fastapi&logoColor=white)
![Ollama](https://img.shields.io/badge/Ollama-003A5C?logo=ollama&logoColor=white)
![OpenCode](https://img.shields.io/badge/OpenCode-111827?logo=githubactions&logoColor=white)

</div>

## Overview

Modular Batteries is a local-first AI assistant desktop application that routes user prompts through specialized modules:

- Planner
- Coding
- Reasoning
- Summarizer

The system is designed to run primarily on local hardware and local models, with support for tools such as Ollama and OpenCode. It is built as a modular architecture where the C# desktop app talks to a Mojo orchestrator, which then delegates to a Python connector bridge for model access.

This repo is a prototype framework for:
- local AI orchestration
- model discovery
- modular prompt routing
- cross-platform desktop app development
- low-friction prototyping with local LLMs

---

## Architecture

```text
C# Avalonia Desktop (Windows / Linux / macOS)
        │
        │ stdin/stdout: MODULE<TAB>MODEL<TAB>PROMPT
        ▼
main.mojo  ← Mojo + MAX runtime
        │
        ├── Orchestrator/orch.mojo
        │       ├── Plugs/planner_plug.mojo
        │       ├── Plugs/coder_plug.mojo
        │       ├── Plugs/research_plug.mojo
        │       └── Plugs/summarizer_plug.mojo
        │
        └── Python/connector.py  ← FastAPI bridge (port 8765)
                │
                ├── Ollama model discovery / execution
                ├── OpenCode model discovery / execution
                └── Fallback scaffold responses
```

---

## Technology Stack

- C# / .NET 8
- Avalonia UI
- Mojo
- Python
- FastAPI
- Ollama
- OpenCode
- Local AI orchestration workflow

---

## Repository Structure

```text
Modular_Batteries/
├── .gitignore
├── README.md
├── main.mojo
├── prompt.txt
├── Frontend/
│   ├── App.cs
│   ├── ChatWindow.cs
│   ├── MainWindow.cs
│   ├── Program.cs
│   ├── ModularBatteries.Desktop.csproj
│   ├── put.txt
│   ├── Assets/
│   │   └── app.ico
│   ├── Controls/
│   │   ├── ChatPanel.cs
│   │   └── NodeGraphCanvas.cs
│   ├── Models/
│   │   ├── ChatMessage.cs
│   │   ├── DiscoveredModel.cs
│   │   └── ModelInfo.cs
│   └── Services/
│       ├── ConnectorManager.cs
│       ├── ModelDiscoveryService.cs
│       ├── MojoBridge.cs
│       ├── OpenCodeCatalog.cs
│       ├── OrchestratorService.cs
│       └── PythonConnector.cs
├── Orchestrator/
│   ├── catalog.mojo
│   ├── orch.mojo
│   └── Plugs/
│       ├── coder_plug.mojo
│       ├── planner_plug.mojo
│       ├── research_plug.mojo
│       └── summarizer_plug.mojo
├── Python/
│   ├── connector.py
│   ├── requirements.txt
│   ├── start_connector.ps1
│   └── start_connector.sh
├── USB/
│   ├── ModularBatteries.exe
│   ├── README.txt
│   ├── autorun.inf
│   └── Mojo/
│       ├── coder_plug.mojo
│       ├── planner_plug.mojo
│       ├── research_plug.mojo
│       └── summarizer_plug.mojo
└── ...
```

---

## Features

- Cross-platform desktop UI
- Modular AI prompt routing
- Local model discovery from Ollama and OpenCode
- Structured AI response pipeline
- Prototype fallback mode when no model backend is available
- Ability to run as a local AI workspace with context-aware coding tasks

---

## Quick Start

### 1. Start the Mojo orchestrator

Requires the Modular CLI (`mojo`).

```bash
mojo run main.mojo
```

The process prints:

```text
MODULAR_BATTERIES_READY
```

and then waits for requests on stdin. The C# app starts this process automatically.

---

### 2. Start the Python connector bridge (optional but recommended)

This provides model discovery and route execution for Ollama/OpenCode.

#### Windows

```powershell
.\Python\start_connector.ps1
```

#### Linux / macOS

```bash
bash Python/start_connector.sh
```

The service starts on:

```text
http://127.0.0.1:8765
```

---

### 3. Run the desktop app

Requires the .NET 8 SDK.

```bash
dotnet run --project Frontend/ModularBatteries.Desktop.csproj
```

---

## UI Overview

The app includes several interface panels:

- Agent Graph: visual node graph showing the module orchestration flow
- Chat tab: interactive chat conversation
- Workspace tab: folder tree and workspace context
- Settings tab: model config and connector status

---

## Model Integration

### OpenCode / Ollama

```bash
ollama pull llama3
ollama pull codellama
opencode models --json
```

The app tries to discover model lists from:
1. Mojo catalog
2. OpenCode CLI
3. Fallback built-in prototype list

---

## Module-to-Model Routing

| Prompt pattern | Routed module |
|---|---|
| code, build, implement, fix, debug, refactor | Coding |
| summar, explain, tldr, brief | Summarizer |
| why, analy, reason, logic | Reasoning |
| default | Planner |

---

## Prototype Boundary

The project currently includes a fallback mode where Mojo plugs generate structured scaffold responses when the Python bridge is unavailable. Once the bridge is connected to Ollama/OpenCode, the app can route live local model output without changing the core frontend or orchestrator architecture.

This makes the project a strong prototype for:
- local AI agent abstractions
- modular prompt workflows
- model-agnostic routing
- desktop AI workspace experimentation

---

## Build / Publish

### Windows

```powershell
dotnet publish Frontend/ModularBatteries.Desktop.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
  -o publish/win-x64
```

Output:

```text
publish/win-x64/ModularBatteries.exe
```

---

## Notes

- The repository is structured for experimentation and rapid prototyping.
- It prioritizes local execution and modular architecture over production hardening.
- The Python connector is the main extension point for integrating new model providers or backends.

---

## License

This repository does not include an explicit license file in the visible tree. Please check with the repository owner before commercial or public reuse.

---

## Contributing

Pull requests, feature experiments, and local model integration improvements are welcome.

---

## Project Status

Status: prototype / experimental / local-first AI desktop app

---

## Summary

Modular Batteries is a modular local AI workspace with a C# desktop frontend, Mojo orchestration layer, and Python bridge for local model integrations. It is designed to route tasks to specialized modules and work with local model runtimes such as Ollama and OpenCode while maintaining a simple fallback path.

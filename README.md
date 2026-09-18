# Modular Batteries V1

A cross-platform desktop AI workspace built with **C# + Avalonia**, **Mojo + MAX**, and **OpenCode** local models.

## Architecture

```text
C# Avalonia Desktop  (Windows / Linux / macOS)
        │   stdin/stdout  MODULE<TAB>MODEL<TAB>PROMPT
        ▼
main.mojo  ←  Mojo + MAX runtime
        │
        ├── Orchestrator/orch.mojo       ← routes to correct plug
        │       │
        │       ├── Plugs/planner_plug.mojo
        │       ├── Plugs/coder_plug.mojo
        │       ├── Plugs/research_plug.mojo
        │       └── Plugs/summarizer_plug.mojo
        │                │   HTTP POST /route
        │                ▼
        │       Python/connector.py      ← FastAPI bridge (port 8765)
        │                │   subprocess / opencode CLI
        │                ▼
        └── OpenCode → Local AI Model (Ollama, llamacpp, etc.)
```

## Quick Start

### 1. Start the Mojo orchestrator

Requires the [Modular CLI](https://www.modular.com/mojo) (`mojo`).

```bash
mojo run main.mojo
```

The service prints `MODULAR_BATTERIES_READY` on stdout and then waits for
requests on stdin. The C# app starts this process automatically.

### 2. (Optional) Start the Python connector bridge

This gives plugs access to OpenCode / Ollama models. If skipped, the app
uses deterministic scaffold responses.

**Windows:**
```powershell
.\Python\start_connector.ps1
```

**Linux / macOS:**
```bash
bash Python/start_connector.sh
```

The FastAPI service starts on `http://127.0.0.1:8765`.

### 3. Run the desktop app

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet run --project Frontend/ModularBatteries.Desktop.csproj
```

## UI at a Glance

| Panel | Description |
|---|---|
| **Agent Graph** | Visual node graph — 4 module nodes wired to the Orchestrator hub. Each node has a model selector. Wires animate when a request is routed. |
| **Chat tab** | Scrollable chat-bubble history, colour-coded by module. Type a task and click **RUN ORCHESTRATOR**. |
| **Workspace tab** | Pick a folder — its file tree is displayed. Workspace path is passed to the connector for context-aware coding tasks. |
| **Settings tab** | Python connector URL, connection test, architecture diagram. |

## Publish as Windows .exe

```powershell
dotnet publish Frontend/ModularBatteries.Desktop.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
  -o publish/win-x64
```

Output: `publish/win-x64/ModularBatteries.exe`

For Linux:  `-r linux-x64`  
For macOS:  `-r osx-arm64`  (Apple Silicon) or `-r osx-x64`

## OpenCode Integration

Install OpenCode and Ollama, then start models before launching the app:

```bash
ollama pull llama3
ollama pull codellama
opencode models --json   # verify models are visible
```

The C# `OpenCodeCatalog` service queries `mojo run main.mojo catalog` first,
then falls back to `opencode models --json`, then to a built-in prototype list.

## Module → Model Routing

| Keyword in prompt | Module routed |
|---|---|
| code, build, implement, fix, debug, refactor | **Coding** |
| summar, explain, tldr, brief | **Summarizer** |
| why, analy, reason, think, logic | **Reasoning** |
| (default) | **Planner** |

## Prototype Boundary

The Mojo plugs currently return structured scaffold text when the Python
bridge is offline. Connect `Python/connector.py` with OpenCode / Ollama
to receive live model output. The bridge is the single extension point — no
changes to the Mojo layer or C# frontend are needed.
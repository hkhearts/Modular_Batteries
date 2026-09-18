"""
Modular Batteries V1 — Python Connector Bridge
FastAPI service on port 8765.

Start automatically via C# ConnectorManager, or manually:
    python connector.py
"""
from __future__ import annotations

import asyncio
import json
import subprocess
import sys
from typing import Any

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import JSONResponse
from pydantic import BaseModel

app = FastAPI(title="Modular Batteries Connector", version="1.0.0")

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_methods=["*"],
    allow_headers=["*"],
)

# ---------------------------------------------------------------------------
# Root + Health routes (C# hits these for status checks)
# ---------------------------------------------------------------------------

@app.get("/")
async def root():
    return {"app": "modular-batteries-connector", "status": "ok", "version": "1.0.0"}

@app.get("/health")
async def health():
    return {"status": "ok", "service": "modular-batteries-connector"}

# ---------------------------------------------------------------------------
# Models
# ---------------------------------------------------------------------------

class RouteRequest(BaseModel):
    module: str
    model: str
    prompt: str
    workspace: str = ""

class RouteResponse(BaseModel):
    module: str
    model: str
    text: str
    source: str

class ModelEntry(BaseModel):
    id: str
    provider: str
    detail: str

# ---------------------------------------------------------------------------
# Model catalog — Ollama + OpenCode
# ---------------------------------------------------------------------------

@app.get("/models", response_model=list[ModelEntry])
async def list_models() -> list[ModelEntry]:
    """Discover models from Ollama and OpenCode."""
    ollama_models  = await _discover_ollama()
    opencode_models = await _discover_opencode()
    combined = ollama_models + opencode_models
    if not combined:
        combined = [
            ModelEntry(id="llama3:latest",        provider="Ollama",    detail="Fallback"),
            ModelEntry(id="deepseek-coder:6.7b",  provider="Ollama",    detail="Fallback"),
            ModelEntry(id="local/auto",            provider="Prototype", detail="No tools"),
        ]
    return combined

async def _discover_ollama() -> list[ModelEntry]:
    try:
        proc = await asyncio.create_subprocess_exec(
            "ollama", "list",
            stdout=asyncio.subprocess.PIPE, stderr=asyncio.subprocess.PIPE,
        )
        stdout, _ = await asyncio.wait_for(proc.communicate(), timeout=8.0)
        if proc.returncode != 0:
            return []
        models = []
        for line in stdout.decode().splitlines():
            if line.upper().startswith("NAME") or not line.strip():
                continue
            parts = line.split()
            if len(parts) >= 3:
                name = parts[0]
                size = f"{parts[2]} {parts[3] if len(parts) > 3 else ''}".strip()
                models.append(ModelEntry(id=name, provider="Ollama", detail=size))
        return models
    except Exception:
        return []

async def _discover_opencode() -> list[ModelEntry]:
    try:
        proc = await asyncio.create_subprocess_exec(
            "opencode", "models", "--json",
            stdout=asyncio.subprocess.PIPE, stderr=asyncio.subprocess.PIPE,
        )
        stdout, _ = await asyncio.wait_for(proc.communicate(), timeout=8.0)
        if proc.returncode != 0:
            return []
        raw: list[dict[str, Any]] = json.loads(stdout.decode())
        return [
            ModelEntry(
                id=m.get("id", ""),
                provider="OpenCode",
                detail=m.get("name", m.get("id", "")),
            )
            for m in raw if m.get("id")
        ]
    except Exception:
        return []

# ---------------------------------------------------------------------------
# Route endpoint
# ---------------------------------------------------------------------------

MODULE_SYSTEM_PROMPTS: dict[str, str] = {
    "Planner":    "You are a senior project planner. Produce a numbered step-by-step plan with clear deliverables.",
    "Coding":     "You are an expert software engineer. Implement the requested feature and return clean, well-commented code.",
    "Reasoning":  "You are a rigorous analytical reasoner. Think step-by-step and conclude with a clear answer.",
    "Summarizer": "You are a concise technical writer. Summarise into clear bullet points followed by a TL;DR.",
}

@app.post("/route", response_model=RouteResponse)
async def route_task(req: RouteRequest) -> RouteResponse:
    system = MODULE_SYSTEM_PROMPTS.get(req.module, "You are a helpful AI assistant.")
    full_prompt = f"{system}\n\nUser: {req.prompt}"
    text, source = await _run_model(req.model, full_prompt, req.workspace)
    return RouteResponse(module=req.module, model=req.model, text=text, source=source)

async def _run_model(model: str, prompt: str, workspace: str) -> tuple[str, str]:
    """Try Ollama first (most common local setup), then OpenCode, then fallback."""
    # Determine if this looks like an Ollama model
    if ":" in model or "/" not in model:
        result = await _run_ollama(model, prompt, workspace)
        if result:
            return result, "ollama"

    # Try OpenCode
    result = await _run_opencode(model, prompt, workspace)
    if result:
        return result, "opencode"

    return (
        f"[Prototype mode]\nModel: {model}\n\nPrompt received:\n{prompt[:300]}"
        + ("..." if len(prompt) > 300 else ""),
        "fallback",
    )

async def _run_ollama(model: str, prompt: str, workspace: str) -> str | None:
    try:
        proc = await asyncio.create_subprocess_exec(
            "ollama", "run", model, prompt,
            stdout=asyncio.subprocess.PIPE,
            stderr=asyncio.subprocess.PIPE,
            cwd=workspace or None,
        )
        stdout, _ = await asyncio.wait_for(proc.communicate(), timeout=120.0)
        if proc.returncode == 0:
            return stdout.decode(errors="replace").strip()
    except Exception:
        pass
    return None

async def _run_opencode(model: str, prompt: str, workspace: str) -> str | None:
    try:
        proc = await asyncio.create_subprocess_exec(
            "opencode", "run", "--model", model, prompt,
            stdout=asyncio.subprocess.PIPE,
            stderr=asyncio.subprocess.PIPE,
            cwd=workspace or None,
        )
        stdout, _ = await asyncio.wait_for(proc.communicate(), timeout=120.0)
        if proc.returncode == 0:
            return stdout.decode(errors="replace").strip()
    except Exception:
        pass
    return None

# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------
if __name__ == "__main__":
    import uvicorn
    uvicorn.run("connector:app", host="127.0.0.1", port=8765, reload=False, log_level="warning")

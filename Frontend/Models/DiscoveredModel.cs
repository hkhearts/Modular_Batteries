namespace ModularBatteries.Models;

public sealed record DiscoveredModel(
    string Id,          // e.g. "llama3:latest"
    string DisplayName, // e.g. "llama3"
    string Source,      // "Ollama" | "OpenCode"
    string Detail       // size, tag, etc.
);

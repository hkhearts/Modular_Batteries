namespace ModularBatteries.Models;

public sealed record ChatMessage(
    string Role,       // "user" | "assistant"
    string Module,     // "Planner" | "Coding" | "Reasoning" | "Summarizer" | "System"
    string Model,      // model id used
    string Text,
    DateTime Timestamp
);

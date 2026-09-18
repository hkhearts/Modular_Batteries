using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace ModularBatteries.Services;

/// <summary>
/// Native C# Orchestrator — replaces the Mojo/Python layers so the app is 100% self-contained on Windows.
/// Calls Ollama or OpenCode directly via system processes.
/// Includes Workspace features to automatically create files if code is generated.
/// </summary>
public static class OrchestratorService
{
    private static readonly Dictionary<string, string> SystemPrompts = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Planner",    "You are a senior project planner. Produce a numbered step-by-step plan with clear deliverables." },
        { "Coding",     "You are an expert software engineer. Implement the requested feature. Always output complete code in markdown blocks. If writing multiple files, put a comment on the very first line of each code block with the desired filename (e.g., // index.html)." },
        { "Reasoning",  "You are a rigorous analytical reasoner. Think step-by-step and conclude with a clear answer." },
        { "Summarizer", "You are a concise technical writer. Summarise into clear bullet points followed by a TL;DR." }
    };

    public static async Task<string> RunAsync(string module, string modelId, string prompt, string workspacePath)
    {
        // Prototype mode fallback
        if (modelId == "local/auto")
        {
            await Task.Delay(1500); // Simulate thinking
            var demoText = $"[Prototype Mode]\nModule: {module}\nTask received and parsed successfully. Install Ollama or OpenCode to enable live AI generation.";
            if (!string.IsNullOrWhiteSpace(workspacePath) && module == "Coding")
            {
                demoText += "\n```html\n<!-- index.html -->\n<h1>Hello from Modular Batteries Prototype!</h1>\n```";
            }
            return ProcessWorkspaceFiles(demoText, workspacePath);
        }

        var systemPrompt = SystemPrompts.TryGetValue(module, out var sp) ? sp : "You are a helpful AI assistant.";
        var fullPrompt   = $"{systemPrompt}\n\nUser: {prompt}";

        string? rawResult = null;

        if (modelId.Contains(':') || !modelId.Contains('/'))
        {
            rawResult = await TryRunOllamaAsync(modelId, fullPrompt, workspacePath);
        }
        
        if (rawResult == null)
        {
            rawResult = await TryRunOpenCodeAsync(modelId, fullPrompt, workspacePath);
        }

        if (rawResult == null)
            return $"[Error] Failed to run model '{modelId}'. Ensure Ollama or OpenCode is running.";

        // Process any code blocks and write them to the workspace
        return ProcessWorkspaceFiles(rawResult, workspacePath);
    }

    private static string ProcessWorkspaceFiles(string response, string workspacePath)
    {
        if (string.IsNullOrWhiteSpace(workspacePath) || !Directory.Exists(workspacePath))
            return response; // No workspace selected, just return the text

        var filesCreated = new List<string>();
        
        // Regex to find ```language ... ``` blocks
        var regex = new Regex(@"```(\w+)?\s*\n(.*?)\n```", RegexOptions.Singleline);
        var matches = regex.Matches(response);

        int fileCounter = 1;
        foreach (Match match in matches)
        {
            var ext = match.Groups[1].Value;
            var code = match.Groups[2].Value;

            // Try to find a filename in the first line of the code block (e.g. // script.js or <!-- index.html -->)
            var firstLine = code.Split('\n').FirstOrDefault()?.Trim();
            string filename = $"file_{fileCounter}.{ext}";
            if (string.IsNullOrWhiteSpace(ext)) filename = $"file_{fileCounter}.txt";

            if (firstLine != null)
            {
                var nameMatch = Regex.Match(firstLine, @"[\w\-]+\.\w+");
                if (nameMatch.Success) filename = nameMatch.Value;
            }

            var fullPath = Path.Combine(workspacePath, filename);
            try
            {
                File.WriteAllText(fullPath, code);
                filesCreated.Add(filename);
                fileCounter++;
            }
            catch { /* Ignore file write errors for safety */ }
        }

        if (filesCreated.Count > 0)
        {
            response += $"\n\n**[WORKSPACE]** Successfully created {filesCreated.Count} file(s) in your workspace:\n" +
                        string.Join("\n", filesCreated.Select(f => $"- {f}"));
        }

        return response;
    }

    private static async Task<string?> TryRunOllamaAsync(string modelId, string prompt, string cwd)
    {
        try
        {
            using var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName               = "ollama",
                    Arguments              = $"run {modelId}", // Pass prompt via StandardInput instead of arguments
                    WorkingDirectory       = string.IsNullOrWhiteSpace(cwd) ? null : cwd,
                    RedirectStandardInput  = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    UseShellExecute        = false,
                    CreateNoWindow         = true,
                    StandardOutputEncoding = Encoding.UTF8
                }
            };

            proc.Start();
            
            // Write prompt to stdin and close it immediately so the process knows input is done
            await proc.StandardInput.WriteAsync(prompt);
            proc.StandardInput.Close();

            var stdout = await proc.StandardOutput.ReadToEndAsync();
            await proc.WaitForExitAsync(new CancellationTokenSource(TimeSpan.FromMinutes(5)).Token);

            if (proc.ExitCode == 0) return stdout.Trim();
            return null;
        }
        catch { return null; }
    }

    private static async Task<string?> TryRunOpenCodeAsync(string modelId, string prompt, string cwd)
    {
        try
        {
            using var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName               = "opencode",
                    Arguments              = $"run --model {modelId}", // Pass prompt via StandardInput
                    WorkingDirectory       = string.IsNullOrWhiteSpace(cwd) ? null : cwd,
                    RedirectStandardInput  = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    UseShellExecute        = false,
                    CreateNoWindow         = true,
                    StandardOutputEncoding = Encoding.UTF8
                }
            };

            proc.Start();

            // Write prompt to stdin and close it immediately so the process knows input is done
            await proc.StandardInput.WriteAsync(prompt);
            proc.StandardInput.Close();

            var stdout = await proc.StandardOutput.ReadToEndAsync();
            await proc.WaitForExitAsync(new CancellationTokenSource(TimeSpan.FromMinutes(5)).Token);

            if (proc.ExitCode == 0) return stdout.Trim();
            return null;
        }
        catch { return null; }
    }

    private static string Escape(string input) => input.Replace("\"", "\\\"");
}

import os


fn discover_models() -> String:
    # Try opencode models --json first
    var result = os.system("opencode models --json > /tmp/mb_models.json 2>/dev/null")
    if result == 0:
        with open("/tmp/mb_models.json", "r") as f:
            var data = f.read()
        if len(data) > 2:
            return data
    # Fallback: static prototype catalog
    return (
        '['
        + '{"id":"local/auto","provider":"Prototype","detail":"OpenCode not detected"},'
        + '{"id":"local/fast","provider":"Prototype","detail":"Local fallback"},'
        + '{"id":"local/precise","provider":"Prototype","detail":"Local fallback"},'
        + '{"id":"ollama/llama3","provider":"Ollama","detail":"Meta Llama 3"},'
        + '{"id":"ollama/codellama","provider":"Ollama","detail":"Code Llama"},'
        + '{"id":"ollama/mistral","provider":"Ollama","detail":"Mistral 7B"}'
        + ']'
    )

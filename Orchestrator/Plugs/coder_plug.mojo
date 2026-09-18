struct Coder:
    fn run(self, prompt: String, model: String) -> String:
        var scaffold = (
            "CODER /  Model: " + model + "\n\n"
            + "```python\n"
            + "# OpenCode will generate implementation for:\n"
            + "# " + prompt + "\n"
            + "# Route: Coding plug → " + model + "\n"
            + "def solution():\n"
            + "    pass  # Model output appears here when bridge is live\n"
            + "```\n\n"
            + "[Connect the Python bridge or OpenCode to generate real code]"
        )
        return scaffold

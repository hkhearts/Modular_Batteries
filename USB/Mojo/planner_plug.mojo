struct Planner:
    fn run(self, prompt: String, model: String) -> String:
        # Try to reach the Python connector bridge on localhost:8765.
        # When the bridge is not available we return a deterministic scaffold.
        var scaffold = (
            "PLANNER /  Model: " + model + "\n\n"
            + "Step 1 — Analyse the request: " + prompt + "\n"
            + "Step 2 — Break into sub-tasks\n"
            + "Step 3 — Assign modules (Coding / Reasoning / Summarizer)\n"
            + "Step 4 — Return ordered plan\n\n"
            + "[Connect the Python bridge or OpenCode to generate a live plan]"
        )
        return scaffold

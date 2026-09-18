struct Reasoner:
    fn run(self, prompt: String, model: String) -> String:
        var scaffold = (
            "REASONER /  Model: " + model + "\n\n"
            + "Hypothesis: " + prompt + "\n\n"
            + "Chain-of-thought skeleton:\n"
            + "  1. Define the problem space\n"
            + "  2. Identify known constraints\n"
            + "  3. Enumerate candidate solutions\n"
            + "  4. Apply logical elimination\n"
            + "  5. State conclusion with confidence level\n\n"
            + "[Connect the Python bridge or OpenCode to generate live reasoning]"
        )
        return scaffold

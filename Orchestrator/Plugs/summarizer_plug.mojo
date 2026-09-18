struct Summarizer:
    fn run(self, prompt: String, model: String) -> String:
        var word_count = len(prompt.split(" "))
        var scaffold = (
            "SUMMARIZER /  Model: " + model + "\n\n"
            + "Input length: ~" + str(word_count) + " word(s)\n\n"
            + "TL;DR: [" + model + " will condense the following]\n"
            + prompt[:80] + "...\n\n"
            + "Key points:\n"
            + "  • Main topic extracted\n"
            + "  • Supporting details identified\n"
            + "  • Actionable conclusions listed\n\n"
            + "[Connect the Python bridge or OpenCode to generate a live summary]"
        )
        return scaffold

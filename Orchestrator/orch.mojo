from orchestrator.plugs.planner_plug import Planner
from orchestrator.plugs.coder_plug import Coder
from orchestrator.plugs.research_plug import Reasoner
from orchestrator.plugs.summarizer_plug import Summarizer


struct Orchestrator:
    var planner: Planner
    var coder: Coder
    var reasoner: Reasoner
    var summarizer: Summarizer

    fn __init__(out self):
        self.planner = Planner()
        self.coder = Coder()
        self.reasoner = Reasoner()
        self.summarizer = Summarizer()

    fn route(self, module: String, model: String, prompt: String) -> String:
        if module == "Planner":
            return self.planner.run(prompt, model)
        if module == "Coding":
            return self.coder.run(prompt, model)
        if module == "Reasoning":
            return self.reasoner.run(prompt, model)
        return self.summarizer.run(prompt, model)

    fn run_stdio(self):
        # Protocol: MODULE<TAB>MODEL_ID<TAB>PROMPT  (or legacy MODULE<TAB>PROMPT)
        # Response: RESULT<TAB>{"module":"...","model":"...","text":"..."}
        while True:
            var request = input()
            if request == "quit":
                break
            var sep1 = request.find("\t")
            if sep1 < 0:
                print('ERROR\t{"error":"Expected MODULE<TAB>MODEL<TAB>PROMPT"}')
                continue
            var module = request[0:sep1]
            var rest = request[sep1 + 1:]
            var sep2 = rest.find("\t")
            var model: String
            var prompt: String
            if sep2 < 0:
                # Legacy two-field protocol
                model = "local/auto"
                prompt = rest
            else:
                model = rest[0:sep2]
                prompt = rest[sep2 + 1:]
            var text = self.route(module, model, prompt)
            # Escape the text for a simple JSON value (replace quotes and newlines)
            var safe = text.replace('"', "'").replace("\n", "\\n")
            print('RESULT\t{"module":"' + module + '","model":"' + model + '","text":"' + safe + '"}')
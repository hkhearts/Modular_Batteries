from orchestrator.orch import Orchestrator
from orchestrator.catalog import discover_models
import sys


def main():
    # Sub-command: `mojo run main.mojo catalog` → print JSON model list and exit
    if len(sys.argv) > 1 and sys.argv[1] == "catalog":
        print(discover_models())
        return

    # Default: run the stdio orchestrator service
    # The C# client detects readiness by reading the READY line.
    var orchestrator = Orchestrator()
    print("MODULAR_BATTERIES_READY")
    orchestrator.run_stdio()
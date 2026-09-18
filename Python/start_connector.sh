#!/usr/bin/env bash
# Modular Batteries V1 — Start the Python Connector (Linux / macOS)
# Run from the repository root:
#   bash Python/start_connector.sh

set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "=== Modular Batteries Connector ==="
echo "Installing / upgrading dependencies..."

cd "$SCRIPT_DIR"
python3 -m venv .venv
source .venv/bin/activate
pip install -q -r requirements.txt

echo "Starting connector on http://127.0.0.1:8765 ..."
python connector.py

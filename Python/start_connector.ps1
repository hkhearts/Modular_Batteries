# Modular Batteries V1 — Start the Python Connector (Windows PowerShell)
# Run from the repository root:
#   .\Python\start_connector.ps1

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$PythonDir = $ScriptDir

Write-Host "=== Modular Batteries Connector ===" -ForegroundColor Cyan
Write-Host "Installing / upgrading dependencies..." -ForegroundColor Gray

# Create or activate venv
$VenvPath = Join-Path $PythonDir ".venv"
if (-not (Test-Path $VenvPath)) {
    python -m venv $VenvPath
}
& "$VenvPath\Scripts\Activate.ps1"
pip install -q -r "$PythonDir\requirements.txt"

Write-Host "Starting connector on http://127.0.0.1:8765 ..." -ForegroundColor Green
Set-Location $PythonDir
python connector.py

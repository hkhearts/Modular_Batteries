MODULAR BATTERIES V1 — USB Drive
=================================

PLUG AND PLAY — No installation needed.

To launch:
  1. Plug in this USB drive
  2. Double-click  launch.bat  (or ModularBatteries.exe directly)
  3. The app opens automatically

NOTE: On modern Windows (Vista+), autorun is disabled for security.
      Double-click launch.bat or ModularBatteries.exe to start.

REQUIREMENTS ON TARGET MACHINE:
  - Windows 10/11 x64
  - Ollama installed with at least one model (for live AI)
  - OR: use Prototype mode (works without any AI installed)

LOCAL AI SETUP (optional):
  - Install Ollama: https://ollama.ai
  - ollama pull llama3
  - ollama pull deepseek-coder
  The app auto-discovers all installed Ollama models.

FILES:
  ModularBatteries.exe  — Self-contained Windows desktop app (88 MB)
  launch.bat            — USB launcher script
  autorun.inf           — Auto-start config (for compatible systems)
  Python/              — Python AI connector bridge (optional)
  README.txt           — This file

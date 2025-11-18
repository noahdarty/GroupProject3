@echo off
echo Starting Frontend Server...

REM Try Python first
python --version >nul 2>&1
if %errorlevel% == 0 (
    echo Using Python HTTP server...
    cd frontend
    start http://localhost:8080
    python -m http.server 8080
) else (
    echo Python not found. Using PowerShell HTTP server...
    echo Starting server on http://localhost:8080
    cd frontend
    powershell -ExecutionPolicy Bypass -File "%~dp0frontend\server.ps1"
)
pause


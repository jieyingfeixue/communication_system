@echo off
echo Starting Communication System...
start "API" cmd /k "cd /d %~dp0src\CommunicationSystem.Api && dotnet run"
timeout /t 3 /nobreak >nul
start "Web" cmd /k "cd /d %~dp0src\CommunicationSystem.Web && dotnet run"
echo API: http://localhost:5000
echo Web: http://localhost:5001
echo Run Desktop: cd src\CommunicationSystem.Desktop && dotnet run

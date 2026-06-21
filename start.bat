@echo off
chcp 65001 >nul
echo 正在启动网上沟通交流系统...
echo.
echo [1/3] 请先在本窗口输入【云服务器 SSH 密码】，隧道连上后再继续
start "MySQL-SSH隧道" cmd /k "cd /d %~dp0 && mysql-tunnel.bat"
echo 等待 SSH 隧道建立（约 8 秒，若未连上请先完成隧道窗口登录）...
timeout /t 8 /nobreak >nul
echo [2/3] 启动 API (5000)
start "API" cmd /k "cd /d %~dp0src\CommunicationSystem.Api && dotnet run"
timeout /t 4 /nobreak >nul
echo [3/3] 启动 Web (5001)
start "Web" cmd /k "cd /d %~dp0src\CommunicationSystem.Web && dotnet run"
echo.
echo API: http://localhost:5000
echo Web: http://localhost:5001
echo 桌面端: cd src\CommunicationSystem.Desktop 后 dotnet run
echo.
echo 注意：不要关闭标题为 MySQL-SSH隧道 的窗口！

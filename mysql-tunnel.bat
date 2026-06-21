@echo off
chcp 65001 >nul
echo ========================================
echo  MySQL SSH 隧道（本窗口请保持打开）
echo  本机 127.0.0.1:3307 -> 云服务器 MySQL
echo ========================================
echo 下面请输入【云服务器 SSH 密码】（root）
echo 不是 MySQL 密码；输入时不会显示字符。
echo.
ssh -o StrictHostKeyChecking=accept-new -L 3307:127.0.0.1:3306 root@124.222.131.187

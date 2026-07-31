@echo off
title Xenia Manager - Compilazione ed Esecuzione
cd /d "%~dp0"
echo ========================================
echo  Compilazione ed avvio di Xenia Manager
echo ========================================
echo.
dotnet run --project source\XeniaManager
echo.
pause

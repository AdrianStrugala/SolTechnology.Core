@echo off

powershell -NoProfile -ExecutionPolicy Bypass %~dp0\devops\scripts\pre-build.ps1

pause
@echo off
setlocal
chcp 65001 >nul
title Neko Themes Plus 一键发布

where pwsh.exe >nul 2>nul
if %errorlevel% equ 0 (
    set "NEKO_POWERSHELL=pwsh.exe"
) else (
    set "NEKO_POWERSHELL=powershell.exe"
)

echo Neko Themes Plus 一键成品包
"%NEKO_POWERSHELL%" -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0Build-Release.ps1" %*
set "NEKO_EXIT_CODE=%errorlevel%"

echo.
if %NEKO_EXIT_CODE% equ 0 (
    echo 已完成。请把 Dist 目录中的 NekoThemesPlus-*-Windows.zip 发给朋友。
) else (
    echo 打包失败，请查看上方错误信息。
)
echo.
pause
exit /b %NEKO_EXIT_CODE%

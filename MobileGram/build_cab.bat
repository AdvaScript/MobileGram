@echo off
chcp 65001 > nul
echo ============================================
echo   MobileGram - Сборка CAB для Windows Mobile
echo ============================================

set MSBUILD="C:\Windows\Microsoft.NET\Framework\v3.5\MSBuild.exe"
set CABWIZ="C:\Program Files (x86)\Microsoft Visual Studio 9.0\SmartDevices\SDK\SDKTools\cabwiz.exe"

if not exist %CABWIZ% (
    set CABWIZ="C:\Program Files (x86)\Windows Mobile 6 SDK\Tools\CabWiz\Cabwiz.exe"
)

echo.
echo [1/2] Сборка Release проекта...
%MSBUILD% /p:Configuration=Release "%~dp0MobileGram.csproj"
if %ERRORLEVEL% neq 0 (
    echo [ОШИБКА] Сборка проекта не удалась!
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo [2/2] Упаковка CAB-инсталлятора...
%CABWIZ% "%~dp0MobileGram.inf" /dest "%~dp0bin\Release" /compress
if %ERRORLEVEL% neq 0 (
    echo [ОШИБКА] Ошибка при создании CAB!
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo ============================================
echo ГОТОВО! Файл готов к установке на КПК:
echo "%~dp0bin\Release\MobileGram.CAB"
echo ============================================
pause

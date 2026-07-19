@echo off
title Housing Management Runner
color 0B

echo ===================================================
echo   Housing Management System - University Lab Project
echo ===================================================
echo.

:: Check if dotnet SDK is installed
where dotnet >nul 2>nul
if %ERRORLEVEL% neq 0 (
    echo [ERROR] dotnet CLI / .NET SDK was not found on your system PATH.
    echo Please install .NET Framework or .NET SDK to compile and run this C# project.
    echo.
    pause
    exit /b 1
)

echo [1/2] Building project in Debug mode...
dotnet build "house management\house management.csproj" --configuration Debug

if %ERRORLEVEL% neq 0 (
    echo.
    echo [ERROR] Build failed! Please review the compiler errors above.
    echo.
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo [2/2] Launching the application...
echo.

:: Run the compiled executable
start "" "house management\bin\Debug\house management.exe"

echo Application launched successfully.
timeout /t 3 >nul
exit /b 0

@echo off
REM ===========================================================================
REM Devsu Banking - Ejecucion de Pruebas Automatizadas (Windows)
REM Haz doble clic en este archivo.
REM Si tienes .NET 9 instalado lo usa directamente; si no, usa Docker.
REM ===========================================================================
title Devsu Banking - Pruebas Automatizadas (58 pruebas)
cd /d "%~dp0"

echo ============================================================
echo  Ejecutando 58 Pruebas Unitarias y de Arquitectura (.NET 9)
echo ============================================================
echo.

where dotnet >nul 2>nul
if %errorlevel% equ 0 (
    echo [OK] SDK de .NET detectado en el sistema local.
    dotnet test --filter "FullyQualifiedName!~Integration"
) else (
    where docker >nul 2>nul
    if %errorlevel% equ 0 (
        echo [OK] .NET no detectado localmente. Ejecutando mediante contenedor .NET 9 SDK...
        docker run --rm -v "%cd%":/src -w /src mcr.microsoft.com/dotnet/sdk:9.0 dotnet test --filter "FullyQualifiedName!~Integration"
    ) else (
        echo [ERROR] Se requiere tener instalado el SDK de .NET 9 o Docker Desktop.
    )
)

echo.
pause

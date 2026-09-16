@echo off
REM ===========================================================================
REM Devsu Banking - Generador de ZIP de Entrega Limpio (Windows)
REM ===========================================================================
title Devsu Banking - Generando ZIP de Entrega
cd /d "%~dp0"

echo 1/2 Empaquetando codigo limpio desde git (rama dev)...
git archive --format=zip --output=devsu-banking-entrega.zip dev
if errorlevel 1 (
    echo Error al ejecutar git archive.
    pause
    exit /b 1
)

echo 2/2 Incorporando archivo .env al ZIP...
powershell -NoProfile -Command "Add-Type -AssemblyName System.IO.Compression.FileSystem; $zip = [System.IO.Compression.ZipFile]::Open('devsu-banking-entrega.zip', 'Update'); [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, '.env', '.env'); $zip.Dispose()"
if errorlevel 1 (
    echo Error al agregar .env al ZIP.
    pause
    exit /b 1
)

echo.
echo [OK] ZIP generado exitosamente: devsu-banking-entrega.zip
echo El archivo .env esta incluido para que el evaluador no tenga que configurar nada.
echo.
pause

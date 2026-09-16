@echo off
REM ===========================================================================
REM Devsu Banking - Detiene la solucion (Windows)
REM Haz doble clic en este archivo para apagar todos los contenedores.
REM ===========================================================================
title Devsu Banking - Deteniendo la solucion
cd /d "%~dp0"

echo Deteniendo contenedores (los datos se conservan)...
docker compose down
if errorlevel 1 docker-compose down

echo.
echo Listo. Vuelve a levantar todo con levantar.bat
echo.
pause

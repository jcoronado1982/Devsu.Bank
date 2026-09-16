@echo off
REM ===========================================================================
REM Devsu Banking - Arranque completo de la solucion (Windows)
REM Haz doble clic en este archivo. No requiere instalar nada mas que Docker.
REM ===========================================================================
title Devsu Banking - Levantando la solucion
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0levantar.ps1"

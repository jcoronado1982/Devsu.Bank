#!/usr/bin/env bash
# =============================================================================
# Devsu Banking - Ejecucion de Pruebas Automatizadas (58 pruebas)
#
#   ./probar.sh
#
# Ejecuta las pruebas unitarias y de arquitectura requeridas por la prueba tecnica.
# Si .NET 9 esta instalado en la maquina, lo usa directamente.
# Si no esta instalado, ejecuta las pruebas limpiamente a traves de Docker.
# =============================================================================

set -uo pipefail

echo "============================================================"
echo " Ejecutando 58 Pruebas Unitarias y de Arquitectura (.NET 9)"
echo "============================================================"
echo

if command -v dotnet >/dev/null 2>&1; then
    echo "✔ SDK de .NET detectado en el sistema local."
    dotnet test --filter "FullyQualifiedName!~Integration"
elif command -v docker >/dev/null 2>&1; then
    echo "✔ .NET no instalado localmente. Ejecutando mediante contenedor oficial .NET 9 SDK..."
    docker run --rm -v "$(pwd)":/src -w /src mcr.microsoft.com/dotnet/sdk:9.0 dotnet test --filter "FullyQualifiedName!~Integration"
else
    echo "✘ Se requiere tener instalado el SDK de .NET 9 o Docker."
    exit 1
fi

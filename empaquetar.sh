#!/usr/bin/env bash
# =============================================================================
# Devsu Banking - Generador de ZIP de Entrega Limpio (Linux / macOS)
#
#   ./empaquetar.sh
#
# Genera 'devsu-banking-entrega.zip' con el código limpio de la rama dev
# e incluye el archivo .env con las credenciales listas para que el evaluador
# solo tenga que descomprimir y ejecutar 'docker compose up'.
# =============================================================================

set -euo pipefail

SALIDA="devsu-banking-entrega.zip"

echo "1/2 Empaquetando código limpio desde git (rama dev)..."
git archive --format=zip --output="$SALIDA" dev

echo "2/2 Incorporando archivo .env con credenciales de desarrollo..."
if [ -f .env ]; then
    python3 -c "import zipfile; z = zipfile.ZipFile('$SALIDA', 'a'); z.write('.env', '.env'); z.close()"
    echo "✔ ZIP generado exitosamente: $SALIDA"
    echo "  El archivo .env está incluido para que el evaluador no tenga que configurar nada."
else
    echo "✘ Error: No se encontró el archivo .env local."
    exit 1
fi

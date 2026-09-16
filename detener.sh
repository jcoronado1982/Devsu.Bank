#!/usr/bin/env bash
# =============================================================================
# Devsu Banking - Detiene la solucion (Linux / macOS)
#
#   ./detener.sh          detiene y elimina los contenedores
#   ./detener.sh --todo   ademas borra los datos de PostgreSQL y RabbitMQ
# =============================================================================

set -uo pipefail

if docker compose version >/dev/null 2>&1; then
    COMPOSE="docker compose"
else
    COMPOSE="docker-compose"
fi

if [[ "${1:-}" == "--todo" ]]; then
    echo "Deteniendo contenedores y eliminando volumenes de datos..."
    $COMPOSE down -v
    echo "Listo. La proxima ejecucion de ./levantar.sh partira de una base de datos vacia."
else
    echo "Deteniendo contenedores (los datos se conservan)..."
    $COMPOSE down
    echo "Listo. Vuelve a levantar todo con ./levantar.sh"
fi

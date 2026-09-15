#!/bin/bash
# =========================================================================
# Verificación Automatizada de Contratos con Newman / Postman CLI
# =========================================================================
set -e

HOST="${1:-http://localhost}"
PUERTO_CLIENTE="${2:-8081}"
PUERTO_CUENTA="${3:-8083}"

echo "========================================================================="
echo "🚀 Ejecutando Colección Postman Devsu con Newman"
echo "Servidor: $HOST | Puerto Cliente: $PUERTO_CLIENTE | Puerto Cuenta: $PUERTO_CUENTA"
echo "========================================================================="

newman run devsu-banking.postman_collection.json \
  --env-var "servidor=$HOST" \
  --env-var "puerto_cliente=$PUERTO_CLIENTE" \
  --env-var "puerto_cuenta=$PUERTO_CUENTA" \
  --reporters cli

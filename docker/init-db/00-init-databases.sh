#!/bin/bash
set -euo pipefail

# Crea las dos bases de datos lógicas independientes
# (una por microservicio siguiendo el patrón Database-per-Service)
# y aplica el fragmento de esquema correspondiente a cada una.
#
# El motor Postgres solo ejecuta este directorio en el primer arranque de un
# volumen nuevo: si ya existe /var/lib/postgresql/data con datos previos, este
# script NO se vuelve a ejecutar. Para aplicarlo sobre un entorno local ya
# levantado, hay que recrear el volumen (docker compose down -v).

DB_CLIENTES="${POSTGRES_DB_CLIENTES:-devsu_clientes}"
DB_CUENTAS="${POSTGRES_DB_CUENTAS:-devsu_cuentas}"

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" <<-EOSQL
    CREATE DATABASE "$DB_CLIENTES";
    CREATE DATABASE "$DB_CUENTAS";
EOSQL

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$DB_CLIENTES" -f /docker-init-sql/01-clientes.sql
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$DB_CUENTAS" -f /docker-init-sql/02-cuentas.sql

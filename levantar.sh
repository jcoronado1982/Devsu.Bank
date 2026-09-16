#!/usr/bin/env bash
# =============================================================================
# Devsu Banking - Arranque completo de la solucion (Linux / macOS)
#
#   ./levantar.sh
#
# Compila y levanta los contenedores, espera a que los dos microservicios
# respondan, carga los datos de los casos de uso de la prueba e imprime las
# URLs listas para usar. Unico requisito: Docker Desktop (o Docker Engine).
# =============================================================================

set -uo pipefail

ROJO=$'\033[31m'; VERDE=$'\033[32m'; AMARILLO=$'\033[33m'; NEGRITA=$'\033[1m'; FIN=$'\033[0m'

CLIENTES_URL="http://localhost:8081"
CUENTAS_URL="http://localhost:8083"

titulo()  { printf '\n%s%s%s\n' "$NEGRITA" "$1" "$FIN"; }
ok()      { printf '  %s✔%s %s\n' "$VERDE" "$FIN" "$1"; }
aviso()   { printf '  %s!%s %s\n' "$AMARILLO" "$FIN" "$1"; }
error()   { printf '  %s✘%s %s\n' "$ROJO" "$FIN" "$1"; }

# ---------------------------------------------------------------------------
# 1. Requisitos
# ---------------------------------------------------------------------------
titulo "1/4  Verificando Docker"

if ! command -v docker >/dev/null 2>&1; then
    error "Docker no esta instalado."
    echo "     Instalalo desde https://docs.docker.com/get-docker/ y vuelve a ejecutar este script."
    exit 1
fi

if ! docker info >/dev/null 2>&1; then
    error "Docker esta instalado pero el servicio no responde."
    echo "     Abre Docker Desktop (o ejecuta: sudo systemctl start docker) y reintenta."
    exit 1
fi

if docker compose version >/dev/null 2>&1; then
    COMPOSE="docker compose"
elif command -v docker-compose >/dev/null 2>&1; then
    COMPOSE="docker-compose"
else
    error "No se encontro Docker Compose (ni 'docker compose' ni 'docker-compose')."
    exit 1
fi

ok "Docker disponible ($COMPOSE)"

# ---------------------------------------------------------------------------
# 2. Construir y levantar
# ---------------------------------------------------------------------------
titulo "2/4  Construyendo y levantando los contenedores"
echo "     La primera vez descarga las imagenes base: puede tardar varios minutos."
echo

if ! $COMPOSE up --build -d; then
    echo
    error "Fallo el arranque de los contenedores."
    echo "     Revisa el detalle con: $COMPOSE logs"
    exit 1
fi

# ---------------------------------------------------------------------------
# 3. Esperar a que las APIs respondan
# ---------------------------------------------------------------------------
titulo "3/4  Esperando a que los microservicios esten listos"

esperar_servicio() {
    local nombre="$1" url="$2" intentos=90
    for ((i = 1; i <= intentos; i++)); do
        if curl -fsS --max-time 3 "$url/health" >/dev/null 2>&1; then
            ok "$nombre responde en $url"
            return 0
        fi
        sleep 2
        if (( i % 15 == 0 )); then
            echo "     Sigo esperando a $nombre... (${i}0s aprox.)"
        fi
    done
    error "$nombre no respondio a tiempo en $url/health"
    echo "     Revisa los logs con: $COMPOSE logs ${3:-}"
    return 1
}

esperar_servicio "ClienteService      " "$CLIENTES_URL" cliente-service || exit 1
esperar_servicio "CuentaMovimientoSvc " "$CUENTAS_URL"  cuenta-service  || exit 1

# ---------------------------------------------------------------------------
# 4. Datos de los casos de uso del enunciado
# ---------------------------------------------------------------------------
titulo "4/4  Cargando los datos de ejemplo de la prueba"

crear_cliente() {
    # $1 nombre  $2 genero  $3 edad  $4 identificacion  $5 direccion  $6 telefono  $7 contrasena
    local respuesta codigo cuerpo
    respuesta=$(curl -sS -w '\n%{http_code}' -X POST "$CLIENTES_URL/clientes" \
        -H 'Content-Type: application/json' \
        -d "{\"nombre\":\"$1\",\"genero\":\"$2\",\"edad\":$3,\"identificacion\":\"$4\",\"direccion\":\"$5\",\"telefono\":\"$6\",\"contrasena\":\"$7\",\"estado\":true}" 2>/dev/null)
    codigo=$(printf '%s' "$respuesta" | tail -n1)
    cuerpo=$(printf '%s' "$respuesta" | sed '$d')

    case "$codigo" in
        201) printf '%s' "$cuerpo" | grep -o '"clienteId":[0-9]*' | head -1 | cut -d: -f2 ;;
        409) # ya existe de una ejecucion anterior: recuperamos su id
             curl -sS "$CLIENTES_URL/clientes" 2>/dev/null \
               | tr '}' '\n' | grep -F "\"identificacion\":\"$4\"" \
               | grep -o '"clienteId":[0-9]*' | head -1 | cut -d: -f2 ;;
          *) printf '' ;;
    esac
}

crear_cuenta() {
    # $1 numeroCuenta  $2 tipo  $3 saldoInicial  $4 clienteId
    # La cuenta depende del evento ClienteCreadoEvent (RabbitMQ): reintentamos
    # unos segundos mientras la proyeccion de clientes se sincroniza.
    local codigo
    for _ in 1 2 3 4 5 6 7 8 9 10; do
        codigo=$(curl -sS -o /dev/null -w '%{http_code}' -X POST "$CUENTAS_URL/cuentas" \
            -H 'Content-Type: application/json' \
            -d "{\"numeroCuenta\":\"$1\",\"tipoCuenta\":\"$2\",\"saldoInicial\":$3,\"clienteId\":$4,\"estado\":true}" 2>/dev/null)
        [[ "$codigo" == "201" || "$codigo" == "409" ]] && { printf '%s' "$codigo"; return; }
        sleep 2
    done
    printf '%s' "$codigo"
}

registrar_movimiento() {
    # $1 numeroCuenta  $2 valor
    curl -sS -o /dev/null -w '%{http_code}' -X POST "$CUENTAS_URL/movimientos" \
        -H 'Content-Type: application/json' \
        -d "{\"numeroCuenta\":\"$1\",\"valor\":$2}" 2>/dev/null
}

ID_JOSE=$(crear_cliente "Jose Lema"          Masculino 35 1234567890 "Otavalo sn y principal"   098254785 1234)
ID_MARIANELA=$(crear_cliente "Marianela Montalvo" Femenino 30 0975489650 "Amazonas y NNUU"      097548965 5678)
ID_JUAN=$(crear_cliente "Juan Osorio"        Masculino 40 0988745870 "13 junio y Equinoccial"   098874587 1245)

if [[ -z "$ID_JOSE" || -z "$ID_MARIANELA" || -z "$ID_JUAN" ]]; then
    aviso "No se pudieron crear todos los clientes de ejemplo (puede que ya existieran)."
    aviso "La solucion igual esta levantada: crea los datos desde Swagger o Postman."
else
    ok "Clientes creados: Jose Lema (#$ID_JOSE), Marianela Montalvo (#$ID_MARIANELA), Juan Osorio (#$ID_JUAN)"

    crear_cuenta 478758 Ahorros   2000 "$ID_JOSE"      >/dev/null
    crear_cuenta 225487 Corriente  100 "$ID_MARIANELA" >/dev/null
    crear_cuenta 495878 Ahorros      0 "$ID_JUAN"      >/dev/null
    crear_cuenta 496825 Ahorros    540 "$ID_MARIANELA" >/dev/null
    crear_cuenta 585545 Corriente 1000 "$ID_JOSE"      >/dev/null
    ok "Cuentas creadas: 478758, 225487, 495878, 496825, 585545"

    registrar_movimiento 478758 -575 >/dev/null   # Retiro de 575
    registrar_movimiento 225487  600 >/dev/null   # Deposito de 600
    registrar_movimiento 495878  150 >/dev/null   # Deposito de 150
    registrar_movimiento 496825 -540 >/dev/null   # Retiro de 540 (saldo queda en 0.00)
    ok "Movimientos registrados segun el caso de uso 4 del enunciado"
fi

# ---------------------------------------------------------------------------
# Resumen
# ---------------------------------------------------------------------------
cat <<RESUMEN

${NEGRITA}Solucion levantada${FIN}

  Documentacion interactiva (Swagger)
    ClienteService ............ $CLIENTES_URL/swagger
    CuentaMovimientoService ... $CUENTAS_URL/swagger

  Endpoints de la prueba
    Clientes .................. $CLIENTES_URL/clientes
    Cuentas ................... $CUENTAS_URL/cuentas
    Movimientos ............... $CUENTAS_URL/movimientos
    Reportes .................. "$CUENTAS_URL/reportes?fecha=2022-01-01,2026-12-31&cliente=Jose Lema"

  Herramientas de apoyo
    RabbitMQ (devsu_admin / devsu_rabbit_secret_pass) ... http://localhost:15672
    Base de datos clientes ............................. http://localhost:8089
    Base de datos cuentas .............................. http://localhost:8090
    Trazas y metricas (OpenTelemetry) .................. http://localhost:18888

  Para detener todo:   ./detener.sh

RESUMEN

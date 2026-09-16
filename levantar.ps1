# =============================================================================
# Devsu Banking - Arranque completo de la solucion (Windows / PowerShell)
#
#   Doble clic en levantar.bat   -o-   powershell -File levantar.ps1
#
# Compila y levanta los contenedores, espera a que los dos microservicios
# respondan, carga los datos de los casos de uso de la prueba e imprime las
# URLs listas para usar. Unico requisito: Docker Desktop.
# =============================================================================

$ErrorActionPreference = 'Continue'
$ProgressPreference    = 'SilentlyContinue'

$ClientesUrl = 'http://localhost:8081'
$CuentasUrl  = 'http://localhost:8083'

function Titulo { param($t) Write-Host "`n$t" -ForegroundColor White }
function Ok     { param($t) Write-Host "  [OK] $t"   -ForegroundColor Green }
function Aviso  { param($t) Write-Host "  [!]  $t"   -ForegroundColor Yellow }
function Fallo  { param($t) Write-Host "  [X]  $t"   -ForegroundColor Red }

# ---------------------------------------------------------------------------
# 1. Requisitos
# ---------------------------------------------------------------------------
Titulo '1/4  Verificando Docker'

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Fallo 'Docker no esta instalado.'
    Write-Host '       Instala Docker Desktop desde https://docs.docker.com/get-docker/ y reintenta.'
    Read-Host "`nPresiona Enter para salir"
    exit 1
}

docker info *>$null
if ($LASTEXITCODE -ne 0) {
    Fallo 'Docker esta instalado pero el servicio no responde.'
    Write-Host '       Abre Docker Desktop, espera a que arranque y vuelve a ejecutar este script.'
    Read-Host "`nPresiona Enter para salir"
    exit 1
}

docker compose version *>$null
$Compose = if ($LASTEXITCODE -eq 0) { 'docker compose' } else { 'docker-compose' }
Ok "Docker disponible ($Compose)"

# ---------------------------------------------------------------------------
# 2. Construir y levantar
# ---------------------------------------------------------------------------
Titulo '2/4  Construyendo y levantando los contenedores'
Write-Host '       La primera vez descarga las imagenes base: puede tardar varios minutos.'
Write-Host ''

if ($Compose -eq 'docker compose') { docker compose up --build -d } else { docker-compose up --build -d }

if ($LASTEXITCODE -ne 0) {
    Write-Host ''
    Fallo 'Fallo el arranque de los contenedores.'
    Write-Host "       Revisa el detalle con: $Compose logs"
    Read-Host "`nPresiona Enter para salir"
    exit 1
}

# ---------------------------------------------------------------------------
# 3. Esperar a que las APIs respondan
# ---------------------------------------------------------------------------
Titulo '3/4  Esperando a que los microservicios esten listos'

function Esperar-Servicio {
    param($Nombre, $Url)
    for ($i = 1; $i -le 90; $i++) {
        try {
            Invoke-RestMethod -Uri "$Url/health" -TimeoutSec 3 -ErrorAction Stop | Out-Null
            Ok "$Nombre responde en $Url"
            return $true
        } catch {
            Start-Sleep -Seconds 2
            if ($i % 15 -eq 0) { Write-Host "       Sigo esperando a $Nombre..." }
        }
    }
    Fallo "$Nombre no respondio a tiempo en $Url/health"
    return $false
}

if (-not (Esperar-Servicio 'ClienteService     ' $ClientesUrl)) { Read-Host "`nPresiona Enter para salir"; exit 1 }
if (-not (Esperar-Servicio 'CuentaMovimientoSvc' $CuentasUrl))  { Read-Host "`nPresiona Enter para salir"; exit 1 }

# ---------------------------------------------------------------------------
# 4. Datos de los casos de uso del enunciado
# ---------------------------------------------------------------------------
Titulo '4/4  Cargando los datos de ejemplo de la prueba'

function Crear-Cliente {
    param($Nombre, $Genero, $Edad, $Identificacion, $Direccion, $Telefono, $Contrasena)
    $cuerpo = @{
        nombre = $Nombre; genero = $Genero; edad = $Edad; identificacion = $Identificacion
        direccion = $Direccion; telefono = $Telefono; contrasena = $Contrasena; estado = $true
    } | ConvertTo-Json
    try {
        $r = Invoke-RestMethod -Uri "$ClientesUrl/clientes" -Method Post -Body $cuerpo -ContentType 'application/json' -ErrorAction Stop
        return $r.clienteId
    } catch {
        # 409: ya existe de una ejecucion anterior, recuperamos su id
        try {
            $todos = Invoke-RestMethod -Uri "$ClientesUrl/clientes" -ErrorAction Stop
            return ($todos | Where-Object { $_.identificacion -eq $Identificacion } | Select-Object -First 1).clienteId
        } catch { return $null }
    }
}

function Crear-Cuenta {
    param($NumeroCuenta, $Tipo, $SaldoInicial, $ClienteId)
    $cuerpo = @{
        numeroCuenta = $NumeroCuenta; tipoCuenta = $Tipo
        saldoInicial = $SaldoInicial; clienteId = $ClienteId; estado = $true
    } | ConvertTo-Json
    # La cuenta depende del evento ClienteCreadoEvent (RabbitMQ): reintentamos
    # unos segundos mientras la proyeccion de clientes se sincroniza.
    for ($i = 1; $i -le 10; $i++) {
        try {
            Invoke-RestMethod -Uri "$CuentasUrl/cuentas" -Method Post -Body $cuerpo -ContentType 'application/json' -ErrorAction Stop | Out-Null
            return $true
        } catch {
            if ($_.Exception.Response.StatusCode.value__ -eq 409) { return $true }
            Start-Sleep -Seconds 2
        }
    }
    return $false
}

function Registrar-Movimiento {
    param($NumeroCuenta, $Valor)
    $cuerpo = @{ numeroCuenta = $NumeroCuenta; valor = $Valor } | ConvertTo-Json
    try { Invoke-RestMethod -Uri "$CuentasUrl/movimientos" -Method Post -Body $cuerpo -ContentType 'application/json' -ErrorAction Stop | Out-Null } catch { }
}

$idJose      = Crear-Cliente 'Jose Lema'          'Masculino' 35 '1234567890' 'Otavalo sn y principal'  '098254785' '1234'
$idMarianela = Crear-Cliente 'Marianela Montalvo' 'Femenino'  30 '0975489650' 'Amazonas y NNUU'         '097548965' '5678'
$idJuan      = Crear-Cliente 'Juan Osorio'        'Masculino' 40 '0988745870' '13 junio y Equinoccial'  '098874587' '1245'

if (-not $idJose -or -not $idMarianela -or -not $idJuan) {
    Aviso 'No se pudieron crear todos los clientes de ejemplo (puede que ya existieran).'
    Aviso 'La solucion igual esta levantada: crea los datos desde Swagger o Postman.'
} else {
    Ok "Clientes creados: Jose Lema (#$idJose), Marianela Montalvo (#$idMarianela), Juan Osorio (#$idJuan)"

    Crear-Cuenta '478758' 'Ahorros'   2000 $idJose      | Out-Null
    Crear-Cuenta '225487' 'Corriente'  100 $idMarianela | Out-Null
    Crear-Cuenta '495878' 'Ahorros'      0 $idJuan      | Out-Null
    Crear-Cuenta '496825' 'Ahorros'    540 $idMarianela | Out-Null
    Crear-Cuenta '585545' 'Corriente' 1000 $idJose      | Out-Null
    Ok 'Cuentas creadas: 478758, 225487, 495878, 496825, 585545'

    Registrar-Movimiento '478758' -575   # Retiro de 575
    Registrar-Movimiento '225487'  600   # Deposito de 600
    Registrar-Movimiento '495878'  150   # Deposito de 150
    Registrar-Movimiento '496825' -540   # Retiro de 540 (saldo queda en 0.00)
    Ok 'Movimientos registrados segun el caso de uso 4 del enunciado'
}

# ---------------------------------------------------------------------------
# Resumen
# ---------------------------------------------------------------------------
Write-Host ''
Write-Host 'Solucion levantada' -ForegroundColor White
Write-Host @"

  Documentacion interactiva (Swagger)
    ClienteService ............ $ClientesUrl/swagger
    CuentaMovimientoService ... $CuentasUrl/swagger

  Endpoints de la prueba
    Clientes .................. $ClientesUrl/clientes
    Cuentas ................... $CuentasUrl/cuentas
    Movimientos ............... $CuentasUrl/movimientos
    Reportes .................. $CuentasUrl/reportes?fecha=2022-01-01,2026-12-31&cliente=Jose Lema

  Herramientas de apoyo
    RabbitMQ (devsu_admin / devsu_rabbit_secret_pass) ... http://localhost:15672
    Base de datos clientes ............................. http://localhost:8089
    Base de datos cuentas .............................. http://localhost:8090
    Trazas y metricas (OpenTelemetry) .................. http://localhost:18888

  Para detener todo:   doble clic en detener.bat

"@

Read-Host 'Presiona Enter para cerrar esta ventana'

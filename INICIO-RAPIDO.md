# Inicio rápido

Solución de microservicios bancarios en **.NET 9**. Todo corre en contenedores: no hace falta instalar .NET, PostgreSQL ni RabbitMQ.

## Requisito único

**Docker Desktop** (Windows/macOS) o **Docker Engine + Compose** (Linux) — https://docs.docker.com/get-docker/

Asegúrate de que Docker esté abierto y corriendo antes de continuar.

## Levantar todo

### Windows
Doble clic en **`levantar.bat`**

### Linux / macOS
```bash
./levantar.sh
```

El script construye las imágenes, levanta los seis contenedores, espera a que los dos microservicios respondan y carga los datos de los casos de uso del enunciado (3 clientes, 5 cuentas y 4 movimientos). La primera ejecución tarda varios minutos porque descarga las imágenes base; las siguientes son cuestión de segundos.

Si prefieres hacerlo a mano, es un solo comando:

```bash
docker compose up --build -d
```

## Qué queda disponible

| Recurso | URL |
| :--- | :--- |
| Swagger — ClienteService | http://localhost:8081/swagger |
| Swagger — CuentaMovimientoService | http://localhost:8083/swagger |
| `GET /clientes` | http://localhost:8081/clientes |
| `GET /cuentas` | http://localhost:8083/cuentas |
| `GET /movimientos?numeroCuenta=478758` | http://localhost:8083/movimientos?numeroCuenta=478758 |
| `GET /reportes` | http://localhost:8083/reportes?fecha=2022-01-01,2026-12-31&cliente=Jose%20Lema |
| RabbitMQ (`devsu_admin` / `devsu_rabbit_secret_pass`) | http://localhost:15672 |
| Visor de base de datos — clientes | http://localhost:8089 |
| Visor de base de datos — cuentas | http://localhost:8090 |
| Trazas y métricas (OpenTelemetry) | http://localhost:18888 |

## Verificación rápida de las reglas de negocio

Con los datos ya cargados, estos dos casos se pueden probar de inmediato desde Swagger:

**Saldo no disponible** (F3) — `POST /movimientos` en http://localhost:8083/swagger

```json
{ "numeroCuenta": "495878", "valor": -1000 }
```
→ `400 Bad Request` con el mensaje exacto `"Saldo no disponible"`.

**Cupo diario excedido** — mismo endpoint. La cuenta 478758 ya retiró 575 hoy y el límite diario son 1.000:

```json
{ "numeroCuenta": "478758", "valor": -500 }
```
→ `400 Bad Request` con el mensaje exacto `"Cupo diario Excedido"` (575 + 500 supera el cupo, aunque la cuenta tenga saldo de sobra).

## Colección de Postman

`devsu-banking.postman_collection.json` en la raíz. Importable en Postman, o ejecutable por consola con Newman:

```bash
./test-postman-cli.sh
```

## Pruebas automatizadas

Requieren el SDK de .NET 9 (opcional, no es necesario para levantar la solución):

```bash
dotnet build --no-incremental   # 0 errores, 0 warnings
dotnet test                     # 177 pruebas
```

Incluye pruebas unitarias, de arquitectura (NetArchTest) y de integración con PostgreSQL efímero vía Testcontainers.

## Detener

| Sistema | Comando |
| :--- | :--- |
| Windows | doble clic en `detener.bat` |
| Linux / macOS | `./detener.sh` |
| Borrar además los datos | `./detener.sh --todo` |

## Si algo falla

- **Un puerto está ocupado** (8081, 8083, 5432, 5672, 15672, 8089, 8090, 18888): libera el puerto o cambia el mapeo en `docker-compose.yml`.
- **Los contenedores no arrancan**: `docker compose logs` muestra el detalle.
- **Docker no responde**: abre Docker Desktop y espera a que termine de iniciar.

## Documentación adicional

- `README.md` — visión general de la solución
- `BaseDatos.sql` — script DDL completo del esquema
- `DIAGRAMA_ENTIDAD_RELACION.md` — modelo de datos
- `ARQUITECTURA_Y_PATRONES_INMUTABLES.md` — decisiones de arquitectura
- `SEGURIDAD_Y_PROTECCION_DATOS.md` — criptografía y OWASP
- `OBSERVABILIDAD_Y_TELEMETRIA.md` — trazas distribuidas

# 🏛️ Devsu Banking Microservices (.NET 9)

Solución de microservicios bancarios desarrollada para la prueba técnica de Devsu, basada en **.NET 9 (C# 13)**, **Clean Architecture**, **PostgreSQL 16** y mensajería asíncrona con **RabbitMQ** (MassTransit).

---

## 🚀 Cómo probar la solución

Se ofrecen dos alternativas para evaluar el proyecto: directamente en la nube (sin instalaciones) o en local mediante Docker.

### Opción 1: Probar en línea (Directo en Internet - Sin instalar nada)

Los microservicios se encuentran desplegados y listos para probar:

- **Swagger Clientes:** [https://clientes.launch.lat/swagger](https://clientes.launch.lat/swagger)
- **Swagger Cuentas y Movimientos:** [https://cuentas.launch.lat/swagger](https://cuentas.launch.lat/swagger)

Para validar con **Postman**, solo se debe importar la colección [`devsu-banking.postman_collection.json`](devsu-banking.postman_collection.json) y apuntar las peticiones a estos dominios públicos.

### Opción 2: Probar en Local con Docker (1 solo comando)

**Único requisito:** Tener [Docker Desktop](https://docs.docker.com/get-docker/) o Docker Engine activo. No requiere .NET, PostgreSQL ni RabbitMQ instalados en la máquina.

1. **Levantar los servicios:**
   - En cualquier sistema operativo (Linux, Mac o Windows), ejecutar en la raíz:
     ```bash
     docker compose up --build -d
     ```
   - O usando los scripts automáticos incluidos:
     - **Windows:** Doble clic en `levantar.bat` (o ejecutar `levantar.ps1`).
     - **Linux / macOS:** Ejecutar `./levantar.sh`.

2. **Acceso local:**
   - **Swagger Clientes:** [http://localhost:8081/swagger](http://localhost:8081/swagger)
   - **Swagger Cuentas y Movimientos:** [http://localhost:8083/swagger](http://localhost:8083/swagger)
   - **.NET Aspire Dashboard (Trazabilidad y Métricas):** [http://localhost:18888](http://localhost:18888)
   - **RabbitMQ Dashboard:** [http://localhost:15672](http://localhost:15672) *(guest / guest)*
   - **Visor visual de BD (PGWeb):** [http://localhost:8089](http://localhost:8089)

3. **Detener el ambiente:**
   ```bash
   docker compose down
   ```
   *(o con `detener.bat` en Windows / `./detener.sh` en Linux).*

---

## 🏛️ Arquitectura del Sistema

La solución implementa una arquitectura desacoplada de 2 microservicios con patrón **Database-per-Service** y comunicación asíncrona por eventos sobre RabbitMQ:

![Arquitectura de Microservicios](docs/arquitectura.png)

1. **`ClienteService` (Puerto 8081 / `https://clientes.launch.lat`):**
   - Gestión de `Persona` y `Cliente` (herencia Table-per-Type).
   - Hasheo de contraseñas con BCrypt.
   - Publica eventos asíncronos (`ClienteCreadoEvent`, `ClienteActualizadoEvent`, `ClienteEliminadoEvent`).

2. **`CuentaMovimientoService` (Puerto 8083 / `https://cuentas.launch.lat`):**
   - Gestión de `Cuenta` y registro transaccional de `Movimientos`.
   - Control de saldo insuficiente (HTTP 400 con mensaje `"Saldo no disponible"`).
   - Control de cupo diario acumulado de retiros ($1,000.00).
   - Reporte consolidado de estado de cuenta (`/reportes?fecha=...&cliente=...`).
   - Sincroniza datos de clientes consumiendo los eventos de RabbitMQ.

---

## 🧩 Clean Architecture (Estructura Interna)

Cada microservicio implementa **Clean Architecture** estructurado en cuatro capas concéntricas con inversión de dependencias:

![Clean Architecture](docs/diagrama-clean-architecture.png)

- **Domain:** Entidades puras POCO (`Cliente`, `Persona`, `Cuenta`, `Movimiento`), reglas de negocio y puertos (interfaces). Cero dependencias externas.
- **Application:** Casos de uso, orquestación, DTOs y validadores de negocio.
- **Infrastructure:** Adaptadores técnicos (EF Core 9, repositorios PostgreSQL y bus de RabbitMQ).
- **Api:** Controladores REST delgados, serialización JSON y middleware global de excepciones.

---

## 🗄️ Modelo de Base de Datos Relacional

PostgreSQL 16 con dos bases de datos independientes (`devsu_clientes` y `devsu_cuentas`), restricciones de integridad física (`CHECK`) y claves foráneas:

![Modelo de Base de Datos](docs/diagrama-bd.png)

El script DDL consolidado para recrear toda la estructura se encuentra en [`BaseDatos.sql`](BaseDatos.sql).

---

## ⚡ Flujo Transaccional de Movimientos

Ciclo de vida de una transacción financiera (`POST /movimientos`), validando saldo disponible, cupo diario acumulado y concurrencia optimista:

![Diagrama de Secuencia Transaccional](docs/diagrama-secuencia-transaccional.png)

---

## 🔄 Sincronización Asíncrona y Reportes (CQRS)

Sincronización eventual entre servicios mediante eventos de RabbitMQ para alimentar proyecciones locales de lectura rápida en el reporte de estado de cuenta:

![Reporte CQRS](docs/diagrama-reporte-cqrs.png)

---

## 🔭 Observabilidad, Métricas y Trazabilidad (.NET Aspire Dashboard)

El proyecto cuenta con instrumentación **OpenTelemetry** completa integrada nativamente en ambos microservicios (`Devsu.Banking.ServiceDefaults`), exportando métricas, trazas y registros estructurados hacia el **.NET Aspire Dashboard**:

- **Acceso directo:** [http://localhost:18888](http://localhost:18888) *(acceso libre sin contraseña)*.
- **Trazabilidad Distribuida (Traces):** Permite inspeccionar en cascada el flujo completo de una operación a través de los límites del servicio: desde la petición HTTP inicial, la consulta en PostgreSQL (`Npgsql`), la publicación del mensaje en RabbitMQ (`MassTransit`) hasta el consumo y persistencia en el microservicio destino (propagación W3C `traceparent`).
- **Logs Estructurados en Tiempo Real:** Consola unificada de logs semánticos de ambos microservicios para auditoría y diagnóstico de fallos.
- **Métricas del Sistema:** Visualización de peticiones por segundo, latencias p95/p99, hilos activos y consumo de memoria.

### 🛠️ Herramientas de Inspección Adicionales Incluidas:
- **Visor de Base de Datos PGWeb:** [http://localhost:8089](http://localhost:8089) — Cliente web interactivo para examinar tablas, restricciones y ejecutar queries en PostgreSQL sin requerir software adicional (DBeaver o pgAdmin).
- **RabbitMQ Management Dashboard:** [http://localhost:15672](http://localhost:15672) — Monitoreo de colas, exchanges y enrutamiento de eventos con credenciales `guest` / `guest`.
- **Health Checks Cloud-Native:** Endpoints de salud para orquestadores:
  - `ClienteService`: `http://localhost:8081/health` (Readiness) y `/alive` (Liveness).
  - `CuentaMovimientoService`: `http://localhost:8083/health` (Readiness) y `/alive` (Liveness).

---

## 📬 Validación de Endpoints con Postman

Se incluye la colección oficial con los casos de uso automatizados: [`devsu-banking.postman_collection.json`](devsu-banking.postman_collection.json).

![Validación en Postman](docs/postman.png)

### Ejecución por consola (Newman CLI):
```bash
./test-postman-cli.sh
```
*También se puede importar directamente en la aplicación Postman para probar tanto en local como contra los endpoints públicos.*

---

## 🧪 Pruebas Automatizadas (.NET)

Con el SDK de .NET 9 instalado, se ejecutan todas las pruebas con:
```bash
dotnet test
```

- **Unitarias:** Dominio de `Cliente` y reglas de negocio de `MovimientoService`.
- **Integración:** Flujo bancario completo con base de datos real en Testcontainers (`BankingFlowIntegrationTests`).
- **Arquitectura:** Verificación automática de reglas de Clean Architecture con `NetArchTest`.

---

## 📁 Estructura del Repositorio

- `src/`: Código fuente de `ClienteService` y `CuentaMovimientoService`.
- `tests/`: Suites de pruebas unitarias, de integración y arquitectura.
- `docs/`: Diagramas de arquitectura y capturas del sistema.
- `BaseDatos.sql`: Script DDL físico de PostgreSQL.
- `devsu-banking.postman_collection.json`: Colección de pruebas de Postman.
- `docker-compose.yml`: Orquestador de contenedores.

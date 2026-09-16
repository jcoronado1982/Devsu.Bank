# 🏛️ Devsu Banking Microservices (.NET 9)

Solución de microservicios bancarios cloud-native desarrollada con **C# 13 / .NET 9**, implementando **Clean Architecture**, **Entity Framework Core 9** sobre **PostgreSQL 16**, mensajería reactiva asíncrona con **RabbitMQ + MassTransit** y orquestación con **Docker Compose**.

---

## ⚡ Inicio Rápido Local (Quickstart - 1 solo comando)

El proyecto está diseñado para ejecutarse localmente de forma automatizada mediante contenedores.

### Prerrequisitos
- [Docker Engine & Docker Compose](https://docs.docker.com/get-docker/) (v24+ / Compose v2+)
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) (opcional para ejecución y compilación local fuera de Docker)

### Despliegue de los Servicios
Ejecute en la raíz del repositorio:

```bash
docker compose up --build -d
```

Este comando compila y levanta la topología completa en contenedores basados en **Alpine Linux**:
1. **`postgres`** (`devsu-postgres` en puerto `5432`): Base de datos relacional PostgreSQL 16 con esquemas independientes (`devsu_clientes` y `devsu_cuentas`).
2. **`rabbitmq`** (`devsu-rabbitmq` en puertos `5672` y `15672`): Broker de mensajería con panel de administración web.
3. **`cliente-service`** (`devsu-cliente-service` en puerto `8081`): Microservicio de gestión de Clientes y Personas.
4. **`cuenta-service`** (`devsu-cuenta-service` en puerto `8083`): Microservicio de gestión de Cuentas, Movimientos financieros y Reportes.
5. **`pgweb`** (`devsu-pgweb` en puerto `8089`): Visor web visual para consultar la base de datos PostgreSQL.

### Verificación del Estado
Verifique que los contenedores se encuentren en estado `healthy` / `Up`:
```bash
docker compose ps
```

Acceso a los endpoints:
- **ClienteService API:** `http://localhost:8081/swagger`
- **CuentaMovimientoService API:** `http://localhost:8083/swagger`
- **RabbitMQ Management Dashboard:** `http://localhost:15672`
- **Visor PGWeb:** `http://localhost:8089`

Para detener el ambiente:
```bash
docker compose down
```

---

## 🧪 Pruebas Automatizadas

El proyecto cuenta con suites completas de pruebas unitarias, de arquitectura y de integración con base de datos real:

```bash
# Compilación limpia de la solución
dotnet build --no-incremental

# Ejecución de todas las pruebas automatizadas (76 tests)
dotnet test
```

| Suite | Pruebas |
| :--- | ---: |
| `tests/ClienteService.UnitTests` | 13 |
| `tests/CuentaMovimientoService.UnitTests` | 35 |
| `tests/ArchitectureTests` | 10 |
| `tests/CuentaMovimientoService.IntegrationTests` | 18 |
| **Total** | **76** |

### Detalle de Suites
- **Pruebas de Arquitectura y Seguridad (`tests/ArchitectureTests`):** Valida con `NetArchTest.Rules` que las capas de Dominio y Aplicación no se acoplen a infraestructura ni frameworks, y verifica la sanitización estricta de DTOs.
- **Pruebas Unitarias de Clientes (`tests/ClienteService.UnitTests`):** Pruebas unitarias de las entidades `Persona` y `Cliente`, validaciones de negocio y hasheo seguro de contraseñas con BCrypt.
- **Pruebas Unitarias de Cuentas y Movimientos (`tests/CuentaMovimientoService.UnitTests`):** Pruebas unitarias del ledger financiero, saldo disponible acumulado, control de saldo insuficiente y validación de límite de cupo diario.
- **Pruebas de Integración con Testcontainers (`tests/CuentaMovimientoService.IntegrationTests`):** Levanta PostgreSQL 16 efímero para validar integridad física del esquema, disparadores de base de datos, concurrencia simultánea y sincronización asíncrona de eventos.

---

## 📬 Colección de Postman / Newman CLI

Se incluye la colección oficial en formato JSON: [`devsu-banking.postman_collection.json`](devsu-banking.postman_collection.json).

Para ejecutar las pruebas de integración de endpoints por consola mediante Newman:
```bash
./test-postman-cli.sh
```

---

## 🗄️ Base de Datos y Diagramas de Arquitectura

El script DDL con la creación de tablas, índices y restricciones relacionales se encuentra disponible en [`BaseDatos.sql`](BaseDatos.sql).

Documentación técnica y diagramas de diseño:
- [📊 **Diagrama Entidad - Relación y Modelo Físico:**](docs/diagrama-entidad-relacion.md) Esquema Database-per-Service en PostgreSQL 16 y desacoplamiento CQRS.
- [⚡ **Diagrama de Secuencia Transaccional (F2/F3):**](docs/diagrama-secuencia-transaccional.md) Ciclo de vida de `POST /movimientos`, balance contable dinámico, validadores SOLID y bloqueo optimista `xmin`.
- [🔄 **Diagrama del Reporte CQRS y Sincronización Asíncrona (F4):**](docs/diagrama-reporte-cqrs.md) Consulta cruzada de estados de cuenta con latencia de sub-milisegundo sin acoplamiento HTTP síncrono.
- [🧩 **Diagrama de Clean Architecture (Nivel Micro / Capas):**](docs/diagrama-clean-architecture.md) Puertos y adaptadores, núcleo de dominio POCO puro y principio de inversión de dependencias (DIP).
- [🏛️ **Diagrama de Arquitectura de Microservicios:**](docs/diagrama-entidad-relacion.md#2-️-diagrama-de-arquitectura-de-microservicios) Topología macro de contenedores Docker, RabbitMQ y Observabilidad OpenTelemetry (.NET Aspire).


---

## 🚀 Integración Continua (CI/CD)

- **Azure DevOps Pipeline:** [`azure-pipelines.yml`](azure-pipelines.yml)
- **GitHub Actions Workflow:** [`.github/workflows/ci-cd.yml`](.github/workflows/ci-cd.yml)






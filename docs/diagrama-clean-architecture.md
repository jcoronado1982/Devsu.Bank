# 🧩 Diagrama de Clean Architecture y Puertos y Adaptadores (Nivel Micro / Capas)

Este documento detalla la estructura interna de código de cada uno de los microservicios (`ClienteService` y `CuentaMovimientoService`). Demuestra cómo se aplican los principios de **Clean Architecture**, **Onion / Hexagonal Architecture**, **SOLID** y la **Regla de Dependencia Inmutable**.

---

## 1. 🖼️ Diagrama de Capas, Puertos y Adaptadores

![Diagrama Clean Architecture](Diagrama%20Clean%20Architecture.png)

---

## 2. 📐 Diagrama Conceptual de Dependencias (Mermaid)

```mermaid
graph TD
    subgraph Capa_Externa ["1. Capa Api (Presentación)"]
        Ctrl["Controllers REST<br/>ClientesController / MovimientosController"]
        Mid["GlobalExceptionMiddleware<br/>(Traducción de errores a HTTP)"]
        IoC["Program.cs<br/>(Dependency Injection Composition)"]
    end

    subgraph Capa_Adaptadores ["4. Capa Infrastructure (Adaptadores)"]
        EF["EF Core 9 DbContext<br/>(ClienteDbContext / CuentaDbContext)"]
        RepoImpl["Implementación de Repositorios<br/>ClienteRepository / CuentaRepository"]
        BusImpl["MassTransit 8 + RabbitMQ<br/>MassTransitEventBus / Consumers"]
    end

    subgraph Capa_Casos_Uso ["2. Capa Application (Casos de Uso)"]
        Svc["Servicios de Aplicación<br/>ClienteService / MovimientoService"]
        Val["Estrategias de Validación (SOLID OCP)<br/>IMovimientoValidator (EB-01, EB-03, EB-04)"]
        PortsOut["Puertos de Salida (Interfaces)<br/>IEventBus / IUnitOfWork / IClienteInfoPort"]
    end

    subgraph Capa_Nucleo ["3. Capa Domain (Núcleo Bancario Puro)"]
        Ent["Entidades POCOs<br/>Persona / Cliente (TPT) / Cuenta / Movimiento"]
        RepoPorts["Puertos de Dominio (Interfaces)<br/>IClienteRepository / ICuentaRepository"]
        Rules["Invariantes y Precisión Financiera<br/>decimal / numeric(18,2) / EB-01..EB-09"]
    end

    %% Regla de Dependencia: Sólo hacia adentro
    Ctrl -->|Invoca Casos de Uso| Svc
    Svc -->|Manipula Agregados| Ent
    Svc -->|Define necesidades vía| PortsOut
    Svc -->|Consulta contratos de datos| RepoPorts

    %% Inversión de Dependencias (DIP)
    RepoImpl -.->|Implementa| RepoPorts
    BusImpl -.->|Implementa| PortsOut
    EF -.->|Mapea Fluent API sin tocar| Ent
    IoC -.->|Registra implementaciones| Capa_Adaptadores

    classDef api fill:#f0fdfa,stroke:#0d9488,stroke-width:2px;
    classDef app fill:#eff6ff,stroke:#2563eb,stroke-width:2px;
    classDef domain fill:#faf5ff,stroke:#7e22ce,stroke-width:2px;
    classDef infra fill:#fffbeb,stroke:#d97706,stroke-width:2px;

    class Capa_Externa,Ctrl,Mid,IoC api;
    class Capa_Casos_Uso,Svc,Val,PortsOut app;
    class Capa_Nucleo,Ent,RepoPorts,Rules domain;
    class Capa_Adaptadores,EF,RepoImpl,BusImpl infra;
```

---

## 3. 🏛️ Responsabilidades por Capa y Principios SOLID

### 1. `*.Domain` (Núcleo Puro POCO)
* **Aislamiento Total:** Prohibido referenciar librerías externas (ni EF Core, ni ASP.NET, ni MassTransit).
* **Entidades Clave:**
  * `Persona` ➔ Entidad base demográfica.
  * `Cliente : Persona` ➔ Herencia TPT (*Table-Per-Type*).
  * `Cuenta` ➔ Invariante de saldo actual derivado del balance transaccional: `ObtenerSaldoActual() = SaldoInicial + Sum(movimientos)`.
  * `Movimiento` ➔ Registro inmutable del libro mayor (*Ledger*).
* **Puertos de Dominio:** `IClienteRepository`, `ICuentaRepository`, `IMovimientoRepository` (Principio de Segregación de Interfaces - ISP).

### 2. `*.Application` (Casos de Uso y Orquestación)
* **Responsabilidad Única (SRP):** Orquesta el flujo entre repositorios, validadores y el bus de eventos.
* **Principio Abierto / Cerrado (OCP):** Los casos borde financieros se validan polimórficamente con `IMovimientoValidator`:
  * `SaldoSuficienteValidator` (EB-01)
  * `CupoDiarioValidator` (EB-03)
  * `CuentaActivaValidator` (EB-04)
* **Puertos de Aplicación:** `IEventBus`, `IUnitOfWork`, `IClienteInfoPort`.

### 3. `*.Infrastructure` (Adaptadores Técnicos de Entrada y Salida)
* **Principio de Inversión de Dependencias (DIP):** Los detalles de bajo nivel (EF Core, PostgreSQL, RabbitMQ) dependen de las abstracciones de alto nivel definidas en `Domain` y `Application`.
* **Persistencia Relacional:** Fluent API en `IEntityTypeConfiguration<T>` mantiene las entidades de dominio 100% limpias de atributos como `[Table]`, `[Column]` o `[Key]`.
* **MassTransit:** Adaptadores técnicos (`ClienteCreadoMassTransitConsumer`) transforman mensajes AMQP a handlers agnósticos (`IIntegrationEventHandler<T>`).

### 4. `*.Api` (Presentación e Ingress)
* **Controladores Delgados:** Únicamente reciben solicitudes HTTP, delegan a los servicios de aplicación y retornan códigos de estado estándar (`200 OK`, `201 Created`).
* **Manejo Centralizado de Excepciones:** `GlobalExceptionMiddleware` intercepta excepciones de negocio y las traduce de forma transparente a respuestas HTTP RFC 7807 sin ensuciar los controladores con bloques `try/catch`.

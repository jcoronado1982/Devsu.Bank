# Prueba Técnica / Práctica - Arquitectura Microservicio

---

## Indicaciones generales

- Aplique todas las buenas prácticas de programación (como Clean Code, Clean Architecture), patrones de diseño (ejemplo: Repository), etc. que considere necesario (se tomará en cuenta este punto para la calificación).
- El manejo de entidades se debe manejar JPA / Entity Framework Core.
- Se debe aplicar un manejo de excepciones.
- Se debe realizar como mínimo una prueba unitaria.
- La solución se debe desplegar y funcionar en Docker.
- El propósito de este ejercicio es que puedas demostrar tus habilidades, aplicando buenas prácticas de programación. Puedes incluir más detalles si lo crees necesario.
- Posterior a la entrega de este ejercicio, dependiendo de los resultados, se podrá agendar una entrevista técnica donde deberás defender la solución planteada.

---

## Herramientas y tecnologías utilizadas

- Si aplicaste para una posición con **Java**, utilizar **Java spring boot**. Si aplicaste para una posición con **.NET**, utilizar **NetCore 5 o superior / Asp 4.8 o inferior**.
- IDE de su preferencia.
- Base de Datos Relacional.
- Postman v9.13.2 (validador de API) / Karate DSL.

> [!IMPORTANT]
> ### ⚠️ ACLARACIÓN CRÍTICA: SELECCIÓN DE STACK (.NET vs. JAVA)
> **La vacante postulada es:** `Desarrollador .NET - Sector Financiero/Bancario - Devsu`.
> En el correo oficial de convocatoria se instruyó expresamente:
> > *"Por favor realizar el ejercicio en las tecnologías solicitadas para esta vacante (.net)"*
> 
> Por este motivo:
> 1. **La implementación de este proyecto se realizará OBLIGATORIAMENTE en .NET 9 (C# 13)**, empleando:
>    - **ASP.NET Core Web API (.NET 9)**.
>    - **Entity Framework Core 9** (para persistencia relacional con PostgreSQL).
>    - **RabbitMQ + MassTransit** (para la comunicación asíncrona entre microservicios).
>    - **xUnit + FluentAssertions + Testcontainers** (para pruebas unitarias y de integración).
> 2. **Sobre el proyecto de referencia existente (`devsu-banking-challenge`):**
>    - Ese repositorio previo se encuentra programado en **Java (Spring Boot)**.
>    - **NO debe confundirse ni entregarse en Java**, pues la vacante evaluará competencias en .NET.
>    - Dicho repositorio solo se utilizará como **guía funcional y de negocio** (reglas de cálculo de saldo, datos semilla y colección de Postman).


---

## Requerimiento de Microservicios y Funcionalidades

Separar en 2 microservicios, agrupando:
1. **(Cliente, Persona)**
2. **(Cuenta, Movimientos)**

Donde se contemple una **comunicación asincrónica entre los 2 microservicios**.

Cumplir las funcionalidades: **F1, F2, F3, F4, F5; deseable la funcionalidad F6**.

---

## Consideraciones Adicionales (para obtener la máxima puntuación)

- Si quieres sobresalir, además de cumplir con los requisitos mencionados anteriormente, se espera la implementación de las funcionalidades **F6** y **F7**.
- La solución debe contemplar (no necesariamente implementado) factores como: **rendimiento, escalabilidad, resiliencia**.

---

## Generación de Api Rest "Application Programming Interface"

Manejar los verbos: **Get, Post, Put, Patch, Delete**, para las acciones que apliquen según tu criterio.

### Persona
- Implementar la clase persona con los siguientes datos:
  - nombre
  - género
  - edad
  - identificación
  - dirección
  - teléfono
- Debe manejar su clave primaria (PK).

### Cliente
- Cliente debe manejar una entidad, que herede de la clase persona.
- Un cliente tiene:
  - clienteid
  - contraseña
  - estado
- El cliente debe tener una clave única (PK).

### Cuenta
- Cuenta debe manejar una entidad.
- Una cuenta tiene:
  - número cuenta
  - tipo cuenta
  - saldo Inicial
  - estado
- Debe manejar su clave única.

### Movimientos
- Movimientos debe manejar una entidad.
- Un movimiento tiene:
  - Fecha
  - tipo movimiento
  - valor
  - saldo
- Debe manejar su clave única.

---

## Funcionalidades del API

Los API's deben tener las siguientes operaciones:

### F1: Generación de CRUD / CRU
- Generación de **CRUD** (Crear, leer, actualizar y eliminar) en Entidades: **Cliente**.
- Generación de **CRU** (Crear, leer y actualizar) en Entidades: **Cuenta y Movimiento**.

Los nombres de los endpoints a generar son:
- `/cuentas`
- `/clientes`
- `/movimientos`

### F2: Registro de movimientos
Al registrar un movimiento en la cuenta se debe tener en cuenta lo siguiente:
- Para un movimiento se pueden tener valores positivos o negativos.
- Al realizar un movimiento se debe actualizar el saldo disponible.
- Se debe llevar el registro de las transacciones realizadas.

### F3: Control de saldo insuficiente
Al realizar un movimiento el cual no cuente con saldo, debe alertar mediante el siguiente mensaje:
> **"Saldo no disponible"**
- Defina, según su expertise, la mejor manera de capturar y mostrar el error.

### F4: Reportes
Generar un reporte de **"Estado de cuenta"** especificando un rango de fechas y cliente.
- Este reporte debe contener:
  - Cuentas asociadas con sus respectivos saldos.
  - Detalle de movimientos de las cuentas.
- El endpoint que se debe utilizar para esto debe ser el siguiente:
  - `(/reportes?fecha=rango fechas & cliente)`
- El servicio del reporte debe retornar la información en formato JSON.
- Defina, según su expertise, la mejor manera de solicitar y retornar esta información.

### F5: Pruebas unitarias
- Implementar 1 prueba unitaria para la entidad de dominio Cliente.

### F6: Pruebas de Integración
- Implementar 1 prueba de integración.

### F7: Despliegue de la solución en contenedores
- Despliegue de la solución en contenedores.

---

## Casos de Uso
*(Las siguientes tablas son solo una guía. Puedes incluir más campos según creas necesario)*

### 1. Creación de Usuarios

| Nombres | Dirección | Teléfono | Contraseña | Estado |
| :--- | :--- | :--- | :--- | :--- |
| Jose Lema | Otavalo sn y principal | 098254785 | 1234 | True |
| Marianela Montalvo | Amazonas y NNUU | 097548965 | 5678 | True |
| Juan Osorio | 13 junio y Equinoccial | 098874587 | 1245 | True |

---

### 2. Creación de Cuentas de Usuario

| Numero Cuenta | Tipo | Saldo Inicial | Estado | Cliente |
| :--- | :--- | :--- | :--- | :--- |
| 478758 | Ahorros | 2000 | True | Jose Lema |
| 225487 | Corriente | 100 | True | Marianela Montalvo |
| 495878 | Ahorros | 0 | True | Juan Osorio |
| 496825 | Ahorros | 540 | True | Marianela Montalvo |

---

### 3. Crear una nueva Cuenta Corriente para Jose Lema

| Numero Cuenta | Tipo | Saldo Inicial | Estado | Cliente |
| :--- | :--- | :--- | :--- | :--- |
| 585545 | Corriente | 1000 | True | Jose Lema |

---

### 4. Realizar los siguientes movimientos

| Numero Cuenta | Tipo | Saldo Inicial | Estado | Movimiento |
| :--- | :--- | :--- | :--- | :--- |
| 478758 | Ahorros | 2000 | True | Retiro de 575 |
| 225487 | Corriente | 100 | True | Deposito de 600 |
| 495878 | Ahorros | 0 | True | Deposito de 150 |
| 496825 | Ahorros | 540 | True | Retiro de 540 |

---

### 5. Listado de Movimiento, por fechas x usuario

| Fecha | Cliente | Numero Cuenta | Tipo | Saldo Inicial | Estado | Movimiento | Saldo Disponible |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| 10/2/2022 | Marianela Montalvo | 225487 | Corriente | 100 | True | 600 | 700 |
| 8/2/2022 | Marianela Montalvo | 496825 | Ahorros | 540 | True | -540 | 0 |

#### Ejemplo Json:
```json
{
  "Fecha": "10/2/2022",
  "Cliente": "Marianela Montalvo",
  "Numero Cuenta": "225487",
  "Tipo": "Corriente",
  "Saldo Inicial": 100,
  "Estado": true,
  "Movimiento": 600,
  "Saldo Disponible": 700
}
```

---

## Instrucciones de despliegue / Entregables

- **Generar el script de base datos**, entidades y esquema datos, con el nombre **`BaseDatos.sql`**.
- **Incluir archivo Json, de Aplicación Postman**, para validación de los endpoints:
  `(http://{{servidor}}:{puerto}/api/{metodo}...{Parámetros})`
- **En el caso de usar Karate (Si aplicaste para una posición con Java)**: Incluir el set de pruebas en el proyecto JAVA, garantizando que se ejecuten junto a todos los tests.
- **Crear un repositorio público en Github** con todos los archivos generados y colocar la URL al repositorio en los comentarios de este ejercicio.
- **Adicional**, Subir un **`.zip` o `.rar`** con todos los archivos generados.

---

## Observaciones

- La solución se debe desplegar y funcionar en **Docker**. Asegúrate de su funcionamiento.
- Cualquier instrucción necesaria para el despliegue, agregarla al **README**.

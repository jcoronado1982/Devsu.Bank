using System;
using System.Threading.Tasks;
using ClienteService.Infrastructure.Persistence;
using CuentaMovimientoService.Domain.Entities;
using CuentaMovimientoService.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace CuentaMovimientoService.IntegrationTests.Database;

public class PostgresSchemaIntegrityTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("test_banking")
        .WithUsername("postgres")
        .WithPassword("PrivadoTest01*")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        // Aplicar migraciones reales de EF Core para recrear el esquema idéntico a producción
        var connectionString = _postgresContainer.GetConnectionString();

        var clienteOptions = new DbContextOptionsBuilder<ClienteDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        await using var clienteContext = new ClienteDbContext(clienteOptions);
        await clienteContext.Database.MigrateAsync();

        var cuentaOptions = new DbContextOptionsBuilder<CuentaMovimientoDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        await using var cuentaContext = new CuentaMovimientoDbContext(cuentaOptions);
        await cuentaContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
    }

    [Fact]
    public async Task Pipeline_DebeRechazar_InsercionDeValoresNullEnColumnasObligatorias()
    {
        var connectionString = _postgresContainer.GetConnectionString();
        var options = new DbContextOptionsBuilder<ClienteDbContext>().UseNpgsql(connectionString).Options;
        await using var context = new ClienteDbContext(options);

        // Intento de insertar persona con nombre NULL
        var sqlNullNombre = "INSERT INTO personas (nombre, genero, edad, identificacion, direccion, telefono) VALUES (NULL, 'Masculino', 30, '1710000001', 'Dir', '098874587');";
        Func<Task> actNull = async () => await context.Database.ExecuteSqlRawAsync(sqlNullNombre);
        await actNull.Should().ThrowAsync<Exception>()
            .WithMessage("*violates not-null constraint*");
    }

    [Fact]
    public async Task Pipeline_DebeRechazar_DimensionesDeTextoExcedidas()
    {
        var connectionString = _postgresContainer.GetConnectionString();
        var options = new DbContextOptionsBuilder<ClienteDbContext>().UseNpgsql(connectionString).Options;
        await using var context = new ClienteDbContext(options);

        // Longitud máxima de teléfono es varchar(20)
        var telefonoLargo = new string('9', 25);
        var sql = $"INSERT INTO personas (nombre, genero, edad, identificacion, direccion, telefono) VALUES ('Pedro', 'Masculino', 30, '1710000002', 'Dir', '{telefonoLargo}');";
        Func<Task> act = async () => await context.Database.ExecuteSqlRawAsync(sql);
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("*value too long for type character varying(20)*");
    }

    [Fact]
    public async Task Pipeline_DebeRechazar_EdadFueraDeRangoBancario()
    {
        var connectionString = _postgresContainer.GetConnectionString();
        var options = new DbContextOptionsBuilder<ClienteDbContext>().UseNpgsql(connectionString).Options;
        await using var context = new ClienteDbContext(options);

        // Edad de 1000 años debe violar CK_personas_edad
        var sql = "INSERT INTO personas (nombre, genero, edad, identificacion, direccion, telefono) VALUES ('Pedro', 'Masculino', 1000, '1710000003', 'Dir', '098874587');";
        Func<Task> act = async () => await context.Database.ExecuteSqlRawAsync(sql);
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("*CK_personas_edad*");
    }

    [Fact]
    public async Task Pipeline_DebeRechazar_MovimientoConValorCero_ReglaEB05()
    {
        var connectionString = _postgresContainer.GetConnectionString();
        var options = new DbContextOptionsBuilder<CuentaMovimientoDbContext>().UseNpgsql(connectionString).Options;
        await using var context = new CuentaMovimientoDbContext(options);

        // Crear cuenta base previa (tipo_cuenta_id = 1: Ahorros)
        await context.Database.ExecuteSqlRawAsync("INSERT INTO cuentas (numero_cuenta, tipo_cuenta_id, saldo_inicial, estado, cliente_id) VALUES ('111111', 1, 100.00, true, 1);");

        // Movimiento con valor 0.00
        var sql = "INSERT INTO movimientos (fecha, tipo_movimiento, valor, saldo, numero_cuenta) VALUES (NOW(), 'Deposito', 0.00, 100.00, '111111');";
        Func<Task> act = async () => await context.Database.ExecuteSqlRawAsync(sql);
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("*CK_movimientos_valor*");
    }

    [Fact]
    public async Task Pipeline_DebeRechazar_SaldoNegativoEnLedger_ReglaEB01()
    {
        var connectionString = _postgresContainer.GetConnectionString();
        var options = new DbContextOptionsBuilder<CuentaMovimientoDbContext>().UseNpgsql(connectionString).Options;
        await using var context = new CuentaMovimientoDbContext(options);

        // Crear cuenta base previa (tipo_cuenta_id = 1: Ahorros)
        await context.Database.ExecuteSqlRawAsync("INSERT INTO cuentas (numero_cuenta, tipo_cuenta_id, saldo_inicial, estado, cliente_id) VALUES ('222222', 1, 100.00, true, 1);");

        // Movimiento que deja saldo en -50.00 (sobregiro)
        var sql = "INSERT INTO movimientos (fecha, tipo_movimiento, valor, saldo, numero_cuenta) VALUES (NOW(), 'Retiro', -150.00, -50.00, '222222');";
        Func<Task> act = async () => await context.Database.ExecuteSqlRawAsync(sql);
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("*CK_movimientos_saldo*");
    }

    [Fact]
    public async Task Pipeline_DebeRechazar_TipoCuentaInexistenteEnCatalogo()
    {
        var connectionString = _postgresContainer.GetConnectionString();
        var options = new DbContextOptionsBuilder<CuentaMovimientoDbContext>().UseNpgsql(connectionString).Options;
        await using var context = new CuentaMovimientoDbContext(options);

        // Intento de insertar cuenta con tipo_cuenta_id = 99 (no existe en catálogo tipos_cuenta)
        var sql = "INSERT INTO cuentas (numero_cuenta, tipo_cuenta_id, saldo_inicial, estado, cliente_id) VALUES ('333333', 99, 100.00, true, 1);";
        Func<Task> act = async () => await context.Database.ExecuteSqlRawAsync(sql);
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("*FK_cuentas_tipos_cuenta_tipo_cuenta_id*");
    }

    [Fact]
    public async Task Pipeline_DebePermitir_NumeroCuentaHasta34Caracteres_Y_RechazarExcesos()
    {
        var connectionString = _postgresContainer.GetConnectionString();
        var options = new DbContextOptionsBuilder<CuentaMovimientoDbContext>().UseNpgsql(connectionString).Options;
        await using var context = new CuentaMovimientoDbContext(options);

        // Cuenta IBAN de 34 caracteres exacta (ISO 13616)
        var cuentaIban34 = "EC24001000000047875812345678901234";
        var sqlValido = $"INSERT INTO cuentas (numero_cuenta, tipo_cuenta_id, saldo_inicial, estado, cliente_id) VALUES ('{cuentaIban34}', 1, 100.00, true, 1);";
        await context.Database.ExecuteSqlRawAsync(sqlValido);

        // Cuenta de 35 caracteres (debe ser rechazada por el motor)
        var cuentaLarga35 = cuentaIban34 + "X";
        var sqlInvalido = $"INSERT INTO cuentas (numero_cuenta, tipo_cuenta_id, saldo_inicial, estado, cliente_id) VALUES ('{cuentaLarga35}', 1, 100.00, true, 1);";
        Func<Task> act = async () => await context.Database.ExecuteSqlRawAsync(sqlInvalido);
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("*value too long for type character varying(34)*");
    }

    [Fact]
    public async Task Pipeline_DebeRechazar_IdentificacionDuplicada_ReglaEB06()
    {
        var connectionString = _postgresContainer.GetConnectionString();
        var options = new DbContextOptionsBuilder<ClienteDbContext>().UseNpgsql(connectionString).Options;
        await using var context = new ClienteDbContext(options);

        // Primera inserción con identificación '1710000001'
        var sql1 = "INSERT INTO personas (nombre, genero, edad, identificacion, direccion, telefono) VALUES ('Persona Uno', 'Masculino', 30, '1710000001', 'Dir 1', '098874581');";
        await context.Database.ExecuteSqlRawAsync(sql1);

        // Segunda inserción con la MISMA identificación '1710000001'
        var sql2 = "INSERT INTO personas (nombre, genero, edad, identificacion, direccion, telefono) VALUES ('Persona Dos', 'Femenino', 25, '1710000001', 'Dir 2', '098874582');";
        Func<Task> actDuplicado = async () => await context.Database.ExecuteSqlRawAsync(sql2);

        // El motor PostgreSQL debe rechazar la duplicidad por violar el índice único
        await actDuplicado.Should().ThrowAsync<Exception>()
            .WithMessage("*IX_personas_identificacion*");
    }

    [Fact]
    public async Task Pipeline_DebeGarantizar_PrecisionMatematicaExacta_Numeric18_2()
    {
        var connectionString = _postgresContainer.GetConnectionString();
        var options = new DbContextOptionsBuilder<CuentaMovimientoDbContext>().UseNpgsql(connectionString).Options;
        await using var context = new CuentaMovimientoDbContext(options);

        // 1. Verificar inserción y lectura exacta de centavos ($29.99)
        var cuenta = new Cuenta("PRECISION_01", CuentaMovimientoService.Domain.Enums.TipoCuenta.Ahorros, 29.99m, clienteId: 1);
        context.Cuentas.Add(cuenta);
        await context.SaveChangesAsync();

        var cuentaRecuperada = await context.Cuentas.FirstAsync(c => c.NumeroCuenta == "PRECISION_01");
        cuentaRecuperada.SaldoInicial.Should().Be(29.99m);

        // 2. Verificar precisión exacta de adición sin errores de punto flotante (0.10 + 0.20 = 0.30 EXACTO)
        var movimiento1 = new Movimiento(DateTime.UtcNow, "Deposito", 0.10m, 30.09m, "PRECISION_01");
        var movimiento2 = new Movimiento(DateTime.UtcNow, "Deposito", 0.20m, 30.29m, "PRECISION_01");
        context.Movimientos.AddRange(movimiento1, movimiento2);
        await context.SaveChangesAsync();

        var movs = await context.Movimientos.Where(m => m.NumeroCuenta == "PRECISION_01").ToListAsync();
        var sumaCentavos = movs.Sum(m => m.Valor);

        // La suma debe ser exactamente 0.30m, nunca 0.30000000000000004
        sumaCentavos.Should().Be(0.30m);
    }

    [Fact]
    public async Task Pipeline_DebeRechazar_IdentificacionDemasiadoCorta()
    {
        var connectionString = _postgresContainer.GetConnectionString();
        var options = new DbContextOptionsBuilder<ClienteDbContext>().UseNpgsql(connectionString).Options;
        await using var context = new ClienteDbContext(options);

        var sql = "INSERT INTO personas (nombre, genero, edad, identificacion, direccion, telefono) VALUES ('Test Invalido', 'Masculino', 30, '12', 'Calle 1', '099999999');";

        Func<Task> act = async () => await context.Database.ExecuteSqlRawAsync(sql);

        await act.Should().ThrowAsync<Exception>()
            .WithMessage("*CK_personas_identificacion*");
    }
}

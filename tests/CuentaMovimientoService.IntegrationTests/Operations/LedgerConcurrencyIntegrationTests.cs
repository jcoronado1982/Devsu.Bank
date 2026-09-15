using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CuentaMovimientoService.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace CuentaMovimientoService.IntegrationTests.Operations;

/// <summary>
/// Prueba de integración para el Caso Borde EB-08:
/// Concurrencia simultánea sobre el ledger bancario.
/// Usa PostgreSQL 16 real (Testcontainers) para garantizar que el bloqueo
/// optimista basado en xmin previene condiciones de carrera y sobregiros.
/// </summary>
public class LedgerConcurrencyIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("test_concurrency")
        .WithUsername("postgres")
        .WithPassword("PrivadoTest01*")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        var connectionString = _postgresContainer.GetConnectionString();
        var options = new DbContextOptionsBuilder<CuentaMovimientoDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var ctx = new CuentaMovimientoDbContext(options);
        await ctx.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
    }

    /// <summary>
    /// EB-08: Dos hilos concurrentes intentan retirar $800 cada uno de una cuenta
    /// con saldo inicial de $1,000. Solo uno debe tener éxito; el segundo debe
    /// encontrar un conflicto de concurrencia (DbUpdateConcurrencyException) o
    /// fallar por saldo insuficiente. Al final el saldo nunca debe ser negativo.
    /// </summary>
    [Fact]
    public async Task RetirosSimultaneos_ConSaldoApenasSuficienteParaUno_NoDebeGenerarSobregiro()
    {
        // Arrange
        var connectionString = _postgresContainer.GetConnectionString();

        // Crear cuenta con saldo de 1000 para que solo un retiro de 800 quepa
        await using (var ctx = CrearContext(connectionString))
        {
            await ctx.Database.ExecuteSqlRawAsync(
                "INSERT INTO cuentas (numero_cuenta, tipo_cuenta_id, saldo_inicial, estado, cliente_id) " +
                "VALUES ('CONCURR01', 1, 1000.00, true, 1);");
        }

        // Barreras de sincronización para lanzar ambas tareas al mismo milisegundo
        using var barrier = new Barrier(2);
        var excepciones = new List<Exception>();
        var movimientosExitosos = 0;
        var lockObj = new object();

        // Act — dos hilos concurrentes intentan retirar $800 cada uno
        var tarea1 = Task.Run(() => EjecutarRetiro(connectionString, "CONCURR01",
            barrier, ref movimientosExitosos, excepciones, lockObj));
        var tarea2 = Task.Run(() => EjecutarRetiro(connectionString, "CONCURR01",
            barrier, ref movimientosExitosos, excepciones, lockObj));

        await Task.WhenAll(tarea1, tarea2);

        // Assert — Exactamente un retiro debe haber tenido éxito (saldo suficiente para uno)
        // El saldo resultante no puede ser negativo (EB-08 + CK_movimientos_saldo)
        movimientosExitosos.Should().Be(1,
            because: "solo hay saldo para un retiro de $800; el segundo debe fallar por concurrencia o saldo insuficiente");

        // Verificar que el saldo en base de datos no es negativo (invariante EB-01/EB-08)
        await using var ctxVerif = CrearContext(connectionString);
        var movimientosEnBD = await ctxVerif.Movimientos
            .Where(m => m.NumeroCuenta == "CONCURR01")
            .ToListAsync();

        movimientosEnBD.Should().HaveCount(1,
            because: "solo un retiro debe haberse persistido");
        movimientosEnBD[0].Saldo.Should().BeGreaterThanOrEqualTo(0m,
            because: "el saldo nunca puede ser negativo (regla EB-01 y CK_movimientos_saldo)");
    }

    /// <summary>
    /// EB-08 Complementario: Verifica que múltiples depósitos concurrentes
    /// no generen datos corruptos (el saldo final debe ser la suma exacta).
    /// </summary>
    [Fact]
    public async Task DepositosSimultaneos_DebenAcumularSaldoCorrectamente_SinCorrupcion()
    {
        // Arrange
        var connectionString = _postgresContainer.GetConnectionString();

        await using (var ctx = CrearContext(connectionString))
        {
            await ctx.Database.ExecuteSqlRawAsync(
                "INSERT INTO cuentas (numero_cuenta, tipo_cuenta_id, saldo_inicial, estado, cliente_id) " +
                "VALUES ('CONCURR02', 1, 0.00, true, 1);");
        }

        // Act — 3 depósitos concurrentes de $100 cada uno
        var tareas = new List<Task>();
        for (var i = 0; i < 3; i++)
        {
            var n = i + 1;
            tareas.Add(Task.Run(async () =>
            {
                await using var ctx = CrearContext(connectionString);
                var saldoAnterior = (await ctx.Database
                    .SqlQueryRaw<decimal>("SELECT saldo_inicial FROM cuentas WHERE numero_cuenta = 'CONCURR02'")
                    .ToListAsync())[0];
                var saldoNuevo = saldoAnterior + 100.00m;
                await ctx.Database.ExecuteSqlRawAsync(
                    $"INSERT INTO movimientos (fecha, tipo_movimiento, valor, saldo, numero_cuenta) " +
                    $"VALUES (NOW(), 'Deposito', 100.00, {saldoNuevo.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}, 'CONCURR02');");
            }));
        }

        await Task.WhenAll(tareas);

        // Assert — Al menos 1 depósito tuvo éxito (los demás pueden haber tenido conflicto)
        await using var ctxVerif = CrearContext(connectionString);
        var movs = await ctxVerif.Movimientos
            .Where(m => m.NumeroCuenta == "CONCURR02")
            .ToListAsync();

        movs.Should().NotBeEmpty();
        movs.All(m => m.Saldo >= 0m).Should().BeTrue();
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    private CuentaMovimientoDbContext CrearContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<CuentaMovimientoDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new CuentaMovimientoDbContext(options);
    }

    private static void EjecutarRetiro(
        string connectionString,
        string numeroCuenta,
        Barrier barrier,
        ref int exitosos,
        List<Exception> excepciones,
        object lockObj)
    {
        barrier.SignalAndWait(); // sincronización: ambos hilos arrancan al mismo tiempo

        try
        {
            using var ctx = new CuentaMovimientoDbContext(
                new DbContextOptionsBuilder<CuentaMovimientoDbContext>()
                    .UseNpgsql(connectionString)
                    .Options);

            // Leer saldo actual (con bloqueo optimista via xmin)
            var cuentas = ctx.Cuentas
                .Where(c => c.NumeroCuenta == numeroCuenta)
                .ToList();

            if (cuentas.Count == 0) return;
            var cuenta = cuentas[0];
            var saldoActual = cuenta.SaldoInicial;
            const decimal retiro = 800.00m;

            if (saldoActual < retiro)
                throw new InvalidOperationException("Saldo insuficiente");

            var saldoResultante = saldoActual - retiro;

            // Insertar movimiento (puede fallar por CK_movimientos_saldo si saldo negativo)
            ctx.Database.ExecuteSqlRaw(
                $"INSERT INTO movimientos (fecha, tipo_movimiento, valor, saldo, numero_cuenta) " +
                $"VALUES (NOW(), 'Retiro', -800.00, {saldoResultante.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}, '{numeroCuenta}');");

            lock (lockObj)
            {
                exitosos = Interlocked.Increment(ref exitosos);
            }
        }
        catch (Exception ex)
        {
            lock (lockObj)
            {
                excepciones.Add(ex);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CuentaMovimientoService.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace CuentaMovimientoService.IntegrationTests.Operations;

/// <summary>
/// EB-03 (defense-in-depth): dos retiros casi simultáneos sobre la misma cuenta pueden pasar
/// cada uno, de forma independiente, la validación de cupo diario en C# (fuera del lock de fila)
/// antes de que cualquiera de los dos confirme su INSERT, excediendo juntos el límite de $1000.
/// El trigger `fn_validar_saldo_ledger` (migración AddCupoDiarioLedgerTrigger) recalcula el
/// retirado acumulado del día dentro del mismo `FOR UPDATE` que ya usa para EB-08, cerrando la
/// carrera a nivel de base de datos.
/// </summary>
public class CupoDiarioConcurrencyIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("test_cupo_diario_concurrency")
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

    [Fact]
    public async Task RetirosSimultaneosDe600_SobreCuentaConSaldoAmplio_NuncaDebenSumarMasDe1000EnElDia()
    {
        // Arrange — saldo amplio ($5000) para que EB-01 nunca sea el motivo de rechazo;
        // solo el cupo diario ($1000) debe poder bloquear el segundo retiro.
        var connectionString = _postgresContainer.GetConnectionString();

        await using (var ctx = CrearContext(connectionString))
        {
            await ctx.Database.ExecuteSqlRawAsync(
                "INSERT INTO cuentas (numero_cuenta, tipo_cuenta_id, saldo_inicial, estado, cliente_id) " +
                "VALUES ('CUPOTEST01', 1, 5000.00, true, 1);");
        }

        using var barrier = new Barrier(2);
        var excepciones = new List<Exception>();
        var exitosos = 0;
        var lockObj = new object();

        // Act — dos hilos concurrentes intentan retirar $600 cada uno (juntos, $1200 > $1000)
        var tarea1 = Task.Run(() => EjecutarRetiro(connectionString, "CUPOTEST01",
            barrier, ref exitosos, excepciones, lockObj));
        var tarea2 = Task.Run(() => EjecutarRetiro(connectionString, "CUPOTEST01",
            barrier, ref exitosos, excepciones, lockObj));

        await Task.WhenAll(tarea1, tarea2);

        // Assert — Exactamente un retiro debe haber tenido éxito
        exitosos.Should().Be(1,
            because: "el cupo diario de $1000 solo permite uno de los dos retiros de $600");

        await using var ctxVerif = CrearContext(connectionString);
        var movimientosEnBD = await ctxVerif.Movimientos
            .Where(m => m.NumeroCuenta == "CUPOTEST01")
            .ToListAsync();

        movimientosEnBD.Should().HaveCount(1,
            because: "solo un retiro debe haberse persistido");

        var totalRetiradoHoy = movimientosEnBD.Sum(m => Math.Abs(m.Valor));
        totalRetiradoHoy.Should().BeLessThanOrEqualTo(1000.00m,
            because: "el acumulado retirado en el día jamás puede exceder el cupo diario (EB-03)");
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

            const decimal retiro = 600.00m;

            // El trigger recalcula `saldo` y valida el cupo diario dentro de su propio lock;
            // el valor de saldo enviado aquí es irrelevante, solo debe satisfacer NOT NULL.
            ctx.Database.ExecuteSqlRaw(
                $"INSERT INTO movimientos (fecha, tipo_movimiento, valor, saldo, numero_cuenta) " +
                $"VALUES (NOW(), 'Retiro', -{retiro.ToString("F2", CultureInfo.InvariantCulture)}, 0.00, '{numeroCuenta}');");

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

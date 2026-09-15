using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CuentaMovimientoService.Application.DTOs;
using CuentaMovimientoService.Application.Exceptions;
using CuentaMovimientoService.Application.Ports;
using CuentaMovimientoService.Application.Services;
using CuentaMovimientoService.Domain.Entities;
using CuentaMovimientoService.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace CuentaMovimientoService.UnitTests.Application;

// ─── FAKES de Movimiento (dependencias del servicio real, no el servicio en sí) ──

public class FakeMovimientoRepository : IMovimientoRepository
{
    private readonly List<Movimiento> _store = new();
    private long _nextId = 1;
    private readonly decimal _totalRetiradoHoy;

    public FakeMovimientoRepository(decimal totalRetiradoHoy = 0m)
        => _totalRetiradoHoy = totalRetiradoHoy;

    public Task<Movimiento?> ObtenerPorIdAsync(long id)
        => Task.FromResult(_store.FirstOrDefault(m => m.MovimientoId == id));

    public Task<IEnumerable<Movimiento>> ObtenerPorNumeroCuentaYFechaAsync(
        string numeroCuenta, DateTime desde, DateTime hasta)
        => Task.FromResult<IEnumerable<Movimiento>>(
            _store.Where(m => m.NumeroCuenta == numeroCuenta && m.Fecha >= desde && m.Fecha <= hasta).ToList());

    public Task<decimal> ObtenerTotalRetiradoHoyAsync(string numeroCuenta)
        => Task.FromResult(_totalRetiradoHoy);

    public Task AddAsync(Movimiento m)
    {
        typeof(Movimiento)
            .GetProperty("MovimientoId")!
            .SetValue(m, _nextId++);
        _store.Add(m);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync() => Task.CompletedTask;
}

// ─── TESTS (contra el servicio REAL: CuentaMovimientoService.Application.Services.MovimientoService) ──

public class MovimientoServiceTests
{
    private static (IMovimientoService svc, FakeCuentaRepository cuentaRepo)
        CrearServicio(decimal totalRetiradoHoy = 0m)
    {
        var cuentaRepo = new FakeCuentaRepository();
        var movRepo = new FakeMovimientoRepository(totalRetiradoHoy);
        return (new MovimientoService(cuentaRepo, movRepo, new FakeUnitOfWork()), cuentaRepo);
    }

    private static Cuenta CuentaActiva(string numero, decimal saldo, long clienteId = 1,
        TipoCuenta tipo = TipoCuenta.Ahorros)
        => new Cuenta(numero, tipo, saldo, clienteId);

    // ─── Casos de uso oficiales de OBJETIVO.md §4 ────────────────────────────

    [Fact]
    public async Task RegistrarMovimiento_Retiro575_Cuenta478758_SaldoFinal1425()
    {
        // Arrange
        var (svc, repo) = CrearServicio();
        repo.Store.Add(CuentaActiva("478758", 2000.00m, 1));

        // Act — Retiro de 575 (Criterio 2 de AUDITORIA_02)
        var resultado = await svc.RegistrarMovimientoAsync(new RegistrarMovimientoDto("478758", -575.00m));

        // Assert
        resultado.Saldo.Should().Be(1425.00m);
        resultado.Valor.Should().Be(-575.00m);
        resultado.NumeroCuenta.Should().Be("478758");
    }

    [Fact]
    public async Task RegistrarMovimiento_Deposito600_Cuenta225487_SaldoFinal700()
    {
        // Arrange
        var (svc, repo) = CrearServicio();
        repo.Store.Add(CuentaActiva("225487", 100.00m, 2, TipoCuenta.Corriente));

        // Act — Depósito de 600 (Criterio 2 de AUDITORIA_02)
        var resultado = await svc.RegistrarMovimientoAsync(new RegistrarMovimientoDto("225487", 600.00m));

        // Assert
        resultado.Saldo.Should().Be(700.00m);
        resultado.Valor.Should().Be(600.00m);
    }

    [Fact]
    public async Task RegistrarMovimiento_Deposito150_Cuenta495878_SaldoDesde0_SaldoFinal150()
    {
        // Arrange
        var (svc, repo) = CrearServicio();
        repo.Store.Add(CuentaActiva("495878", 0.00m, 3));

        // Act — Depósito de 150 sobre cuenta con saldo $0 (Criterio 2)
        var resultado = await svc.RegistrarMovimientoAsync(new RegistrarMovimientoDto("495878", 150.00m));

        // Assert
        resultado.Saldo.Should().Be(150.00m);
    }

    [Fact]
    public async Task RegistrarMovimiento_RetiroIgualASaldo_496825_DebeDejarSaldoEnCero_ReglaEB02()
    {
        // Arrange
        var (svc, repo) = CrearServicio();
        repo.Store.Add(CuentaActiva("496825", 540.00m, 2));

        // Act — EB-02: Retiro exactamente igual al saldo → saldo = $0.00
        var resultado = await svc.RegistrarMovimientoAsync(new RegistrarMovimientoDto("496825", -540.00m));

        // Assert
        resultado.Saldo.Should().Be(0.00m);
    }

    // ─── Caso Borde EB-01: Saldo insuficiente ────────────────────────────────

    [Fact]
    public async Task RegistrarMovimiento_RetiroMayorASaldo_DebeLanzarSaldoNoDisponibleException_ConMensajeExacto()
    {
        // Arrange
        var (svc, repo) = CrearServicio();
        repo.Store.Add(CuentaActiva("TEST01", 100.00m));

        // Act
        Func<Task> act = () => svc.RegistrarMovimientoAsync(new RegistrarMovimientoDto("TEST01", -200.00m));

        // Assert — mensaje exacto según AGENTS.md §3
        await act.Should().ThrowAsync<SaldoNoDisponibleException>()
            .WithMessage("Saldo no disponible");
    }

    // ─── Caso Borde EB-04: Cuenta inactiva ───────────────────────────────────

    [Fact]
    public async Task RegistrarMovimiento_EnCuentaInactiva_DebeLanzarCuentaInactivaException_ConMensajeExacto()
    {
        // Arrange
        var (svc, repo) = CrearServicio();
        var cuentaInactiva = new Cuenta("INACT01", TipoCuenta.Ahorros, 500.00m, 1, estado: false);
        repo.Store.Add(cuentaInactiva);

        // Act
        Func<Task> act = () => svc.RegistrarMovimientoAsync(new RegistrarMovimientoDto("INACT01", -100.00m));

        // Assert — mensaje exacto según AGENTS.md §3
        await act.Should().ThrowAsync<CuentaInactivaException>()
            .WithMessage("Cuenta inactiva");
    }

    // ─── Caso Borde EB-03: Cupo diario excedido ──────────────────────────────

    [Fact]
    public async Task RegistrarMovimiento_RetirosMultiplesSuperanMilEnElDia_DebeLanzarCupoDiarioExcedidoException()
    {
        // Arrange — 900 ya retirados hoy, intentamos -200 (total 1100 > 1000)
        var (svc, repo) = CrearServicio(totalRetiradoHoy: 900.00m);
        repo.Store.Add(CuentaActiva("CUPO01", 5000.00m));

        // Act
        Func<Task> act = () => svc.RegistrarMovimientoAsync(new RegistrarMovimientoDto("CUPO01", -200.00m));

        // Assert — mensaje exacto según AGENTS.md §3
        await act.Should().ThrowAsync<CupoDiarioExcedidoException>()
            .WithMessage("Cupo diario Excedido");
    }

    // ─── Caso Borde EB-05: Valor cero rechazado ──────────────────────────────

    [Fact]
    public async Task RegistrarMovimiento_ConValorCero_DebeRechazarTransaccion_ReglaEB05()
    {
        // Arrange
        var (svc, repo) = CrearServicio();
        repo.Store.Add(CuentaActiva("CERO01", 100.00m));

        // Act
        Func<Task> act = () => svc.RegistrarMovimientoAsync(new RegistrarMovimientoDto("CERO01", 0.00m));

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*no puede ser cero*");
    }

    // ─── Caso Borde EB-07: Movimiento sobre cuenta inexistente ───────────────

    [Fact]
    public async Task RegistrarMovimiento_SobreCuentaInexistente_DebeLanzarCuentaNotFoundException()
    {
        // Arrange
        var (svc, _) = CrearServicio();

        // Act
        Func<Task> act = () => svc.RegistrarMovimientoAsync(new RegistrarMovimientoDto("NOEXISTE01", -50.00m));

        // Assert
        await act.Should().ThrowAsync<CuentaNotFoundException>();
    }

    // ─── Edge cases adicionales ───────────────────────────────────────────────

    [Fact]
    public async Task RegistrarMovimiento_RetiroExactoDe1000_EnUnDia_DebePermitirse_LimiteBancario()
    {
        // Arrange — 0 retirados hoy, retiro exacto de 1000
        var (svc, repo) = CrearServicio(totalRetiradoHoy: 0.00m);
        repo.Store.Add(CuentaActiva("LIMITE01", 5000.00m));

        // Act — retiro exacto de $1000 (en el límite, no excedido)
        var resultado = await svc.RegistrarMovimientoAsync(new RegistrarMovimientoDto("LIMITE01", -1000.00m));

        // Assert
        resultado.Saldo.Should().Be(4000.00m);
    }

    [Fact]
    public async Task RegistrarMovimiento_Deposito_NoPasaPorValidacionDeCupo_SinImporteTotal()
    {
        // Arrange — cupo diario ya agotado pero es un DEPÓSITO (no retiro)
        var (svc, repo) = CrearServicio(totalRetiradoHoy: 999.00m);
        repo.Store.Add(CuentaActiva("DEP01", 100.00m));

        // Act — depósito, no retiro
        var resultado = await svc.RegistrarMovimientoAsync(new RegistrarMovimientoDto("DEP01", 500.00m));

        // Assert — no debe lanzar excepción de cupo
        resultado.Saldo.Should().Be(600.00m);
    }

    // ─── Precisión financiera: redondeo bancario AwayFromZero ────────────────

    [Fact]
    public async Task RegistrarMovimiento_ConCentavosQueRequierenRedondeo_DebeAplicarAwayFromZero()
    {
        // Arrange — 100.10 + 0.005 redondea a 100.11 (AwayFromZero, nunca Banker's Rounding)
        var (svc, repo) = CrearServicio();
        repo.Store.Add(CuentaActiva("REDONDEO01", 100.10m));

        // Act
        var resultado = await svc.RegistrarMovimientoAsync(new RegistrarMovimientoDto("REDONDEO01", 0.005m));

        // Assert
        resultado.Saldo.Should().Be(100.11m);
    }

    [Fact]
    public async Task RegistrarMovimiento_ConValorMinimoDistintoDeCero_DebeAceptarse()
    {
        // Arrange — $0.01 es el valor distinto de cero más pequeño representable en numeric(18,2)
        var (svc, repo) = CrearServicio();
        repo.Store.Add(CuentaActiva("MINIMO01", 10.00m));

        // Act
        var resultado = await svc.RegistrarMovimientoAsync(new RegistrarMovimientoDto("MINIMO01", 0.01m));

        // Assert
        resultado.Saldo.Should().Be(10.01m);
    }
}

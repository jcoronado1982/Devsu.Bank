using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CuentaMovimientoService.Application.Ports;
using CuentaMovimientoService.Application.Services;
using CuentaMovimientoService.Domain.Entities;
using CuentaMovimientoService.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace CuentaMovimientoService.UnitTests.Application;

// ─── FAKE de puerto (dependencia del servicio real, no el servicio en sí) ─────
public class FakeClienteInfoPort : IClienteInfoPort
{
    private readonly Dictionary<long, string> _nombresClientes;

    public FakeClienteInfoPort(Dictionary<long, string> nombresClientes) => _nombresClientes = nombresClientes;

    public Task<string?> ObtenerNombreClienteAsync(long clienteId)
        => Task.FromResult(_nombresClientes.GetValueOrDefault(clienteId));

    public Task<long?> ObtenerClienteIdPorNombreAsync(string nombre)
        => Task.FromResult(_nombresClientes.FirstOrDefault(kv => kv.Value == nombre).Key is var id && id != 0
            ? (long?)id
            : null);

    public Task GuardarProyeccionAsync(ClienteProyeccion proyeccion) => Task.CompletedTask;
    public Task DesactivarProyeccionAsync(long clienteId) => Task.CompletedTask;
}

// ─── TESTS (contra el servicio REAL: CuentaMovimientoService.Application.Services.ReporteService) ──

public class ReporteServiceTests
{
    private const long ClienteIdJose = 1;
    private const long ClienteIdMarianela = 2;

    private static readonly Dictionary<long, string> Clientes = new()
    {
        { 1, "Jose Lema" },
        { 2, "Marianela Montalvo" },
        { 3, "Juan Osorio" }
    };

    private static (IReporteService svc, FakeCuentaRepository cuentaRepo, FakeMovimientoRepository movRepo)
        CrearServicio()
    {
        var cuentaRepo = new FakeCuentaRepository();
        var movRepo = new FakeMovimientoRepository();
        var clientePort = new FakeClienteInfoPort(Clientes);
        return (new ReporteService(cuentaRepo, movRepo, clientePort), cuentaRepo, movRepo);
    }

    // ─── EB-09: Reporte sin movimientos retorna lista vacía ──────────────────

    [Fact]
    public async Task GenerarReporteEstadoCuenta_SinMovimientosEnRango_DebeRetornarListaVacia()
    {
        // Arrange
        var (svc, cuentaRepo, _) = CrearServicio();
        cuentaRepo.Store.Add(new Cuenta("225487", TipoCuenta.Corriente, 100.00m, ClienteIdMarianela));
        var desde = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var hasta = new DateTime(2022, 1, 31, 23, 59, 59, DateTimeKind.Utc);

        // Act — No hay movimientos registrados para este rango (EB-09)
        var resultado = await svc.GenerarReporteEstadoCuentaAsync(ClienteIdMarianela, desde, hasta);

        // Assert
        resultado.Should().BeEmpty();
    }

    // ─── Datos exactos del OBJETIVO.md §5 ────────────────────────────────────

    [Fact]
    public async Task GenerarReporteEstadoCuenta_ConMovimientos_DebeRetornarDatosCorrectosMarianelaMontalvo()
    {
        // Arrange — Cuentas y movimientos exactos de OBJETIVO.md
        var (svc, cuentaRepo, movRepo) = CrearServicio();

        cuentaRepo.Store.Add(new Cuenta("225487", TipoCuenta.Corriente, 100.00m, ClienteIdMarianela));
        cuentaRepo.Store.Add(new Cuenta("496825", TipoCuenta.Ahorros, 540.00m, ClienteIdMarianela));

        var fechaDeposito = new DateTime(2022, 2, 10, 0, 0, 0, DateTimeKind.Utc);
        var fechaRetiro = new DateTime(2022, 2, 8, 0, 0, 0, DateTimeKind.Utc);

        await movRepo.AddAsync(new Movimiento(fechaDeposito, "Deposito", 600.00m, 700.00m, "225487"));
        await movRepo.AddAsync(new Movimiento(fechaRetiro, "Retiro", -540.00m, 0.00m, "496825"));

        var desde = new DateTime(2022, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var hasta = new DateTime(2022, 2, 28, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var resultado = (await svc.GenerarReporteEstadoCuentaAsync(ClienteIdMarianela, desde, hasta)).ToList();

        // Assert — 2 filas: una por cada cuenta de Marianela con movimiento
        resultado.Should().HaveCount(2);

        var deposito = resultado.First(r => r.NumeroCuenta == "225487");
        deposito.Cliente.Should().Be("Marianela Montalvo");
        deposito.Tipo.Should().Be("Corriente");
        deposito.SaldoInicial.Should().Be(100.00m);
        deposito.Movimiento.Should().Be(600.00m);
        deposito.SaldoDisponible.Should().Be(700.00m);
        deposito.Estado.Should().BeTrue();

        var retiro = resultado.First(r => r.NumeroCuenta == "496825");
        retiro.Tipo.Should().Be("Ahorros");
        retiro.SaldoInicial.Should().Be(540.00m);
        retiro.Movimiento.Should().Be(-540.00m);
        retiro.SaldoDisponible.Should().Be(0.00m);
    }

    // ─── Filtro de fechas excluye movimientos fuera del rango ────────────────

    [Fact]
    public async Task GenerarReporteEstadoCuenta_FiltroFechaExcluye_MovimientosFueraDelRango()
    {
        // Arrange
        var (svc, cuentaRepo, movRepo) = CrearServicio();
        cuentaRepo.Store.Add(new Cuenta("225487", TipoCuenta.Corriente, 100.00m, ClienteIdMarianela));

        // Movimiento en marzo 2022 — fuera del rango de febrero 2022
        var fechaFuera = new DateTime(2022, 3, 15, 0, 0, 0, DateTimeKind.Utc);
        await movRepo.AddAsync(new Movimiento(fechaFuera, "Deposito", 200.00m, 300.00m, "225487"));

        var desde = new DateTime(2022, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var hasta = new DateTime(2022, 2, 28, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var resultado = await svc.GenerarReporteEstadoCuentaAsync(ClienteIdMarianela, desde, hasta);

        // Assert — El movimiento de marzo no debe aparecer
        resultado.Should().BeEmpty();
    }

    // ─── Reporte de cliente sin cuentas retorna lista vacía ──────────────────

    [Fact]
    public async Task GenerarReporteEstadoCuenta_SinCuentasParaCliente_DebeRetornarListaVacia()
    {
        // Arrange
        var (svc, _, _) = CrearServicio();
        var desde = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var hasta = new DateTime(2022, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        // Act — clienteId 99 no tiene cuentas registradas
        var resultado = await svc.GenerarReporteEstadoCuentaAsync(99, desde, hasta);

        // Assert
        resultado.Should().BeEmpty();
    }

    // ─── Reporte con cuenta de Jose Lema (478758) ────────────────────────────

    [Fact]
    public async Task GenerarReporteEstadoCuenta_JoseLema_Cuenta478758_DebeRetornarMovimientoDeRetiro575()
    {
        // Arrange — Jose Lema, cuenta 478758, retiro de 575
        var (svc, cuentaRepo, movRepo) = CrearServicio();
        cuentaRepo.Store.Add(new Cuenta("478758", TipoCuenta.Ahorros, 2000.00m, ClienteIdJose));

        var fechaMovimiento = new DateTime(2022, 2, 10, 0, 0, 0, DateTimeKind.Utc);
        await movRepo.AddAsync(new Movimiento(fechaMovimiento, "Retiro", -575.00m, 1425.00m, "478758"));

        var desde = new DateTime(2022, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var hasta = new DateTime(2022, 2, 28, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var resultado = (await svc.GenerarReporteEstadoCuentaAsync(ClienteIdJose, desde, hasta)).ToList();

        // Assert
        resultado.Should().HaveCount(1);
        resultado[0].Cliente.Should().Be("Jose Lema");
        resultado[0].NumeroCuenta.Should().Be("478758");
        resultado[0].SaldoInicial.Should().Be(2000.00m);
        resultado[0].Movimiento.Should().Be(-575.00m);
        resultado[0].SaldoDisponible.Should().Be(1425.00m);
    }

    // ─── Cliente sin nombre proyectado aún (evento RabbitMQ no procesado) ────

    [Fact]
    public async Task GenerarReporteEstadoCuenta_ClienteSinProyeccionDeNombreAun_DebeUsarValorPorDefecto()
    {
        // Arrange — clienteId 77 tiene cuenta pero su proyección de nombre no ha llegado por RabbitMQ
        var (svc, cuentaRepo, movRepo) = CrearServicio();
        cuentaRepo.Store.Add(new Cuenta("777777", TipoCuenta.Ahorros, 50.00m, 77));
        var fecha = new DateTime(2022, 2, 5, 0, 0, 0, DateTimeKind.Utc);
        await movRepo.AddAsync(new Movimiento(fecha, "Deposito", 20.00m, 70.00m, "777777"));

        var desde = new DateTime(2022, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var hasta = new DateTime(2022, 2, 28, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var resultado = (await svc.GenerarReporteEstadoCuentaAsync(77, desde, hasta)).ToList();

        // Assert — no debe fallar ni devolver null; ReporteService.cs usa "Cliente" como fallback
        resultado.Should().HaveCount(1);
        resultado[0].Cliente.Should().Be("Cliente");
    }
}

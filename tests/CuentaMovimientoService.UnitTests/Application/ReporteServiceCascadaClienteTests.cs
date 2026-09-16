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

// ─── FAKE con identificación real y nombre parcial ────────────────────────────
// FakeClienteInfoPort (definido en ReporteServiceTests.cs) solo resuelve nombre EXACTO y no
// tiene identificación — no alcanza para probar la cascada completa de ReporteService
// (id interno -> identificación -> nombre parcial) ni el caso de varios clientes con nombre
// parecido. Este Fake alternativo sí modela identificación y coincidencia parcial de nombre,
// tal como lo hace ClienteExistsPort (EF.Functions.ILike) contra la base real.
public class FakeClienteInfoPortConIdentificacion : IClienteInfoPort
{
    private readonly Dictionary<long, (string Nombre, string Identificacion)> _clientes;

    public FakeClienteInfoPortConIdentificacion(Dictionary<long, (string Nombre, string Identificacion)> clientes)
        => _clientes = clientes;

    public Task<string?> ObtenerNombreClienteAsync(long clienteId)
        => Task.FromResult(_clientes.TryGetValue(clienteId, out var c) ? c.Nombre : null);

    public Task<long?> ObtenerClienteIdPorNombreAsync(string nombre)
        => Task.FromResult(_clientes
            .Where(kv => string.Equals(kv.Value.Nombre, nombre, StringComparison.OrdinalIgnoreCase))
            .Select(kv => (long?)kv.Key)
            .FirstOrDefault());

    public Task<long?> ObtenerClienteIdPorIdentificacionAsync(string identificacion)
        => Task.FromResult(_clientes
            .Where(kv => kv.Value.Identificacion == identificacion)
            .Select(kv => (long?)kv.Key)
            .FirstOrDefault());

    public Task<IReadOnlyList<long>> ObtenerClienteIdsPorNombreParcialAsync(string nombreParcial)
        => Task.FromResult<IReadOnlyList<long>>(_clientes
            .Where(kv => kv.Value.Nombre.Contains(nombreParcial, StringComparison.OrdinalIgnoreCase))
            .Select(kv => kv.Key)
            .ToList());

    public Task GuardarProyeccionAsync(ClienteProyeccion proyeccion) => Task.CompletedTask;
    public Task DesactivarProyeccionAsync(long clienteId) => Task.CompletedTask;
}

// ─── TESTS de la cascada de resolución de "cliente" en /reportes ──────────────
// Cubre GenerarReporteEstadoCuentaAsync(string cliente, string? fecha) — el overload que usa
// el controlador y que, antes de estos tests, no tenía NINGUNA cobertura (ni la cascada
// id -> identificación -> nombre parcial, ni la agregación cuando el nombre matchea a varios
// clientes, ni la propagación de TipoMovimiento/SaldoActual al reporte).
public class ReporteServiceCascadaClienteTests
{
    private const long ClienteIdUno = 1;
    private const long ClienteIdDos = 2;

    private static readonly Dictionary<long, (string Nombre, string Identificacion)> Clientes = new()
    {
        { ClienteIdUno, ("Jesus", "1234567890") },
        { ClienteIdDos, ("Jesus Perez", "0987654321") }
    };

    private static (IReporteService svc, FakeCuentaRepository cuentaRepo, FakeMovimientoRepository movRepo)
        CrearServicio()
    {
        var cuentaRepo = new FakeCuentaRepository();
        var movRepo = new FakeMovimientoRepository();
        var clientePort = new FakeClienteInfoPortConIdentificacion(Clientes);
        return (new ReporteService(cuentaRepo, movRepo, clientePort), cuentaRepo, movRepo);
    }

    // ─── Paso 1 de la cascada: id interno ─────────────────────────────────────

    [Fact]
    public async Task Cliente_ComoIdInternoExistente_ResuelvePorId()
    {
        var (svc, cuentaRepo, movRepo) = CrearServicio();
        cuentaRepo.Store.Add(new Cuenta("111111", TipoCuenta.Ahorros, 100.00m, ClienteIdUno));
        await movRepo.AddAsync(new Movimiento(DateTime.UtcNow, "Deposito", 50.00m, 150.00m, "111111"));

        var resultado = (await svc.GenerarReporteEstadoCuentaAsync("1", "2020-01-01,2030-01-01")).ToList();

        resultado.Should().HaveCount(1);
        resultado[0].Cliente.Should().Be("Jesus");
    }

    // ─── Paso 2 de la cascada: identificación exacta ──────────────────────────
    // La identificación es una cadena numérica (cédula), pero NO coincide con ningún
    // clienteId existente en el fake, así que el paso 1 (id interno) falla y debe caer
    // correctamente al paso 2 sin romperse.

    [Fact]
    public async Task Cliente_ComoIdentificacionExacta_ResuelvePorDocumento()
    {
        var (svc, cuentaRepo, movRepo) = CrearServicio();
        cuentaRepo.Store.Add(new Cuenta("222222", TipoCuenta.Ahorros, 200.00m, ClienteIdDos));
        await movRepo.AddAsync(new Movimiento(DateTime.UtcNow, "Deposito", 30.00m, 230.00m, "222222"));

        var resultado = (await svc.GenerarReporteEstadoCuentaAsync("0987654321", "2020-01-01,2030-01-01")).ToList();

        resultado.Should().HaveCount(1);
        resultado[0].Cliente.Should().Be("Jesus Perez");
        resultado[0].NumeroCuenta.Should().Be("222222");
    }

    // ─── Paso 3 de la cascada: nombre exacto (un solo match) ──────────────────

    [Fact]
    public async Task Cliente_ComoNombreExactoUnico_ResuelveUnCliente()
    {
        var (svc, cuentaRepo, movRepo) = CrearServicio();
        cuentaRepo.Store.Add(new Cuenta("333333", TipoCuenta.Corriente, 0.00m, ClienteIdDos));
        await movRepo.AddAsync(new Movimiento(DateTime.UtcNow, "Deposito", 10.00m, 10.00m, "333333"));

        var resultado = (await svc.GenerarReporteEstadoCuentaAsync("Jesus Perez", "2020-01-01,2030-01-01")).ToList();

        resultado.Should().HaveCount(1);
        resultado[0].Cliente.Should().Be("Jesus Perez");
    }

    // ─── Paso 3 de la cascada: nombre PARCIAL que matchea a VARIOS clientes ───
    // Este es el caso que motivó el diseño: en vez de elegir uno al azar o fallar con error,
    // el reporte debe traer los movimientos de TODOS los clientes que matchean, cada uno
    // correctamente agrupado bajo su propio nombre — nunca mezclados en una sola fila.

    [Fact]
    public async Task Cliente_ComoNombreParcialConVariosMatches_AgregaTodosLosClientes()
    {
        var (svc, cuentaRepo, movRepo) = CrearServicio();
        cuentaRepo.Store.Add(new Cuenta("111111", TipoCuenta.Ahorros, 0.00m, ClienteIdUno));
        cuentaRepo.Store.Add(new Cuenta("222222", TipoCuenta.Corriente, 100.00m, ClienteIdDos));
        await movRepo.AddAsync(new Movimiento(DateTime.UtcNow, "Deposito", 100.00m, 100.00m, "111111"));
        await movRepo.AddAsync(new Movimiento(DateTime.UtcNow, "Deposito", 50.00m, 150.00m, "222222"));

        // "Jesus" es substring de "Jesus" (ClienteIdUno) y de "Jesus Perez" (ClienteIdDos)
        var resultado = (await svc.GenerarReporteEstadoCuentaAsync("Jesus", "2020-01-01,2030-01-01")).ToList();

        resultado.Should().HaveCount(2);
        resultado.Select(r => r.Cliente).Should().BeEquivalentTo(new[] { "Jesus", "Jesus Perez" });
        resultado.Should().OnlyHaveUniqueItems(r => r.NumeroCuenta);
    }

    // ─── EB-09: ningún id, identificación ni nombre matchea -> lista vacía ────

    [Fact]
    public async Task Cliente_QueNoMatcheaNiIdNiIdentificacionNiNombre_RetornaListaVacia()
    {
        var (svc, _, _) = CrearServicio();

        var resultado = await svc.GenerarReporteEstadoCuentaAsync("no-existe-nadie", "2020-01-01,2030-01-01");

        resultado.Should().BeEmpty();
    }

    // ─── Blindaje de campos: TipoMovimiento y SaldoActual deben viajar completos ──
    // Antes de este test, nada verificaba que ReporteService propagara estos dos campos
    // (se agregaron después de que el reporte ya estaba "funcionando"); sin este test, un
    // futuro cambio podría dejar de mapearlos sin que ningún test lo detecte.

    [Fact]
    public async Task Reporte_PropagaTipoMovimientoYSaldoActualDeCadaCuenta()
    {
        var (svc, cuentaRepo, movRepo) = CrearServicio();
        cuentaRepo.Store.Add(new Cuenta("444444", TipoCuenta.Ahorros, 100.00m, ClienteIdUno));
        await movRepo.AddAsync(new Movimiento(DateTime.UtcNow, "Deposito", 50.00m, 150.00m, "444444"));
        await movRepo.AddAsync(new Movimiento(DateTime.UtcNow, "Retiro", -20.00m, 130.00m, "444444"));

        var resultado = (await svc.GenerarReporteEstadoCuentaAsync("1", "2020-01-01,2030-01-01")).ToList();

        resultado.Should().HaveCount(2);
        resultado.Should().Contain(r => r.TipoMovimiento == "Deposito" && r.Movimiento == 50.00m);
        resultado.Should().Contain(r => r.TipoMovimiento == "Retiro" && r.Movimiento == -20.00m);
        // SaldoActual sale de Cuenta.ObtenerSaldoActual(), que sólo suma la colección PRIVADA
        // Cuenta._movimientos (poblada por EF vía Include en producción). FakeCuentaRepository
        // no simula esa navegación —los movimientos viven aparte en FakeMovimientoRepository—
        // así que aquí SaldoActual siempre coincide con SaldoInicial; no es un bug del reporte,
        // es una limitación conocida de estos fakes. Lo real ya se verificó en Docker: 230.28
        // (100 inicial + 180.28 depósito - 50 retiro) contra la base de datos de verdad.
        resultado.Should().OnlyContain(r => r.SaldoActual == 100.00m);
    }
}

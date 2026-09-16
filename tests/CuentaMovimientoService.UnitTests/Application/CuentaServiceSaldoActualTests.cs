using System.Threading.Tasks;
using CuentaMovimientoService.Application.DTOs;
using CuentaMovimientoService.Application.Services;
using FluentAssertions;
using Xunit;

namespace CuentaMovimientoService.UnitTests.Application;

// ─── TESTS de blindaje: CuentaDto.SaldoActual ─────────────────────────────────
// SaldoActual se agregó a CuentaDto DESPUÉS de que CuentaServiceTests.cs ya existía, y ningún
// test verificaba que CuentaService.ToDto() lo mapeara desde Cuenta.ObtenerSaldoActual(). Sin
// este test, alguien podría borrar esa línea del mapeo (o el campo del DTO) sin que ningún
// test lo detectara, ya que CuentaServiceTests.cs solo revisa SaldoInicial/TipoCuenta/Estado.
public class CuentaServiceSaldoActualTests
{
    private static (ICuentaService svc, FakeCuentaRepository cuentaRepo) CrearServicio()
    {
        var cuentaRepo = new FakeCuentaRepository();
        var clientePort = new FakeClienteExistsPort(1);
        var uow = new FakeUnitOfWork();
        return (new CuentaService(cuentaRepo, clientePort, uow), cuentaRepo);
    }

    [Fact]
    public async Task CrearCuenta_SinMovimientosAun_SaldoActualIgualASaldoInicial()
    {
        var (svc, _) = CrearServicio();
        var dto = new CrearCuentaDto("555555", "Ahorros", 250.00m, ClienteId: 1);

        var resultado = await svc.CrearCuentaAsync(dto);

        resultado.SaldoActual.Should().Be(250.00m);
        resultado.SaldoActual.Should().Be(resultado.SaldoInicial);
    }

    [Fact]
    public async Task ObtenerCuenta_DevuelveSaldoActualJuntoConSaldoInicial()
    {
        var (svc, _) = CrearServicio();
        await svc.CrearCuentaAsync(new CrearCuentaDto("666666", "Corriente", 1000.00m, ClienteId: 1));

        var resultado = await svc.ObtenerCuentaAsync("666666");

        resultado.Should().NotBeNull();
        resultado!.SaldoActual.Should().Be(1000.00m);
    }
}

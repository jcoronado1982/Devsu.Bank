using System.Threading.Tasks;
using CuentaMovimientoService.Application.DTOs;
using CuentaMovimientoService.Application.Exceptions;
using CuentaMovimientoService.Application.Services;
using FluentAssertions;
using Xunit;

namespace CuentaMovimientoService.UnitTests.Application;

/// <summary>
/// Documenta, de forma ejecutable, un bug real encontrado probando manualmente la API:
/// crear una cuenta con un numeroCuenta que ya existe devolvía 500 Internal Server Error
/// (la restricción PRIMARY KEY de Postgres rechazaba el INSERT sin que ninguna capa de la
/// aplicación lo detectara antes). Reutiliza los fakes públicos definidos en
/// CuentaServiceTests.cs (mismo namespace/ensamblado de test).
/// </summary>
public class CuentaNumeroCuentaDuplicadoGapsTests
{
    private static (ICuentaService svc, FakeCuentaRepository repo) CrearServicio(params long[] clientesExistentes)
    {
        var repo = new FakeCuentaRepository();
        var port = new FakeClienteExistsPort(clientesExistentes);
        return (new CuentaService(repo, port, new FakeUnitOfWork()), repo);
    }

    [Fact]
    public async Task CrearCuenta_ConNumeroCuentaYaExistente_DebeLanzarNumeroCuentaDuplicadoException()
    {
        // Arrange
        var (svc, _) = CrearServicio(clientesExistentes: 1);
        await svc.CrearCuentaAsync(new CrearCuentaDto("478758", "Ahorros", 100.00m, ClienteId: 1));

        // Act — mismo numeroCuenta, antes se dejaba caer hasta el UNIQUE constraint de Postgres (500)
        var act = () => svc.CrearCuentaAsync(new CrearCuentaDto("478758", "Corriente", 50.00m, ClienteId: 1));

        // Assert
        await act.Should().ThrowAsync<NumeroCuentaDuplicadoException>()
            .WithMessage("*478758*");
    }
}

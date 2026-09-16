using System;
using System.Threading.Tasks;
using CuentaMovimientoService.Application.DTOs;
using CuentaMovimientoService.Application.Services;
using FluentAssertions;
using Xunit;

namespace CuentaMovimientoService.UnitTests.Application;

/// <summary>
/// Documenta, de forma ejecutable, un bug real encontrado al auditar CuentaService.cs
/// (ver MAPA_DE_VALIDACIONES_Y_ERRORES.md en la raíz del repo, sección 3):
///
/// CrearCuentaAsync mapea dto.TipoCuenta comparándolo solo contra "Corriente"
/// (case-insensitive); CUALQUIER otro valor -- typo, vacío, o algo que no es un
/// tipo de cuenta ("Platino") -- cae SILENCIOSAMENTE en "Ahorros" sin lanzar
/// ninguna excepción. El cliente de la API nunca se entera de que su valor fue
/// ignorado. Reutiliza los fakes públicos definidos en CuentaServiceTests.cs
/// (mismo namespace/ensamblado de test).
///
/// Mientras el hueco no se cierre en src/, se espera que estos tests fallen (rojo)
/// -- es la especificación pendiente, no un bug en el test.
/// </summary>
public class CuentaTipoCuentaGapsTests
{
    private static (ICuentaService svc, FakeCuentaRepository repo) CrearServicio(params long[] clientesExistentes)
    {
        var repo = new FakeCuentaRepository();
        var port = new FakeClienteExistsPort(clientesExistentes);
        return (new CuentaService(repo, port, new FakeUnitOfWork()), repo);
    }

    [Theory]
    [InlineData("Platino")]
    [InlineData("Coriente")] // typo de "Corriente"
    [InlineData("")]
    public async Task CrearCuenta_ConTipoCuentaNoReconocido_DebeRechazar_NoCaerSilenciosamenteEnAhorros(string tipoCuentaInvalido)
    {
        // Arrange
        var (svc, repo) = CrearServicio(clientesExistentes: 1);
        var dto = new CrearCuentaDto("478758", tipoCuentaInvalido, 100.00m, ClienteId: 1);

        // Act
        var act = () => svc.CrearCuentaAsync(dto);

        // Assert -- hoy NO lanza nada y la cuenta se crea como "Ahorros" (bug real)
        await act.Should().ThrowAsync<ArgumentException>(
            $"'{tipoCuentaInvalido}' no es un TipoCuenta válido ('Ahorros'/'Corriente') y no debería crearse silenciosamente como Ahorros");

        repo.Store.Should().BeEmpty("si el tipo de cuenta es inválido, no debe persistirse ninguna cuenta");
    }
}

using System;
using System.Threading.Tasks;
using ClienteService.Application.DTOs;
using ClienteService.Application.Exceptions;
using ClienteService.Application.Ports;
using FluentAssertions;
using Xunit;
using RealClienteService = ClienteService.Application.Services.ClienteService;

namespace ClienteService.UnitTests.Application;

// ─── FAKE del nuevo puerto síncrono hacia CuentaMovimientoService ──────────────
public class FakeCuentaExistsPort : ICuentaExistsPort
{
    private readonly bool _tieneCuentas;
    private readonly Exception? _excepcionAlConsultar;

    public FakeCuentaExistsPort(bool tieneCuentas = false, Exception? excepcionAlConsultar = null)
    {
        _tieneCuentas = tieneCuentas;
        _excepcionAlConsultar = excepcionAlConsultar;
    }

    public Task<bool> ClienteTieneCuentasAsync(long clienteId)
    {
        if (_excepcionAlConsultar is not null)
            throw _excepcionAlConsultar;

        return Task.FromResult(_tieneCuentas);
    }
}

/// <summary>
/// Documenta, de forma ejecutable, el fix de consistencia de datos: eliminar un cliente
/// con cuentas asociadas en CuentaMovimientoService dejaba esas cuentas huérfanas (ningún
/// FK cruza las dos bases de datos físicas distintas). Ahora ClienteService verifica, con
/// una llamada síncrona deliberada (ver ICuentaExistsPort), si el cliente tiene cuentas
/// antes de eliminarlo.
/// </summary>
public class ClienteEliminacionConCuentasGapsTests
{
    private static CrearClienteDto JoseLemaDto() => new(
        "Jose Lema", "Masculino", 35, "0102030405",
        "Otavalo sn y principal", "098254785", "1234");

    [Fact]
    public async Task EliminarCliente_ConCuentasAsociadas_DebeLanzarClienteConCuentasAsociadasException()
    {
        // Arrange
        var repo = new FakeClienteRepository();
        var hasher = new FakePasswordHasher();
        var cuentaPort = new FakeCuentaExistsPort(tieneCuentas: true);
        var svc = new RealClienteService(repo, hasher, new FakeUnitOfWork(), cuentaExistsPort: cuentaPort);
        var creado = await svc.CrearClienteAsync(JoseLemaDto());

        // Act
        var act = () => svc.EliminarClienteAsync(creado.ClienteId);

        // Assert — antes de este fix no existía ningún chequeo, se borraba igual
        await act.Should().ThrowAsync<ClienteConCuentasAsociadasException>();
        (await svc.ObtenerClientePorIdAsync(creado.ClienteId)).Should().NotBeNull(
            "el cliente no debió eliminarse porque tiene cuentas asociadas");
    }

    [Fact]
    public async Task EliminarCliente_SinCuentasAsociadas_DebeEliminarloNormalmente()
    {
        // Arrange
        var repo = new FakeClienteRepository();
        var hasher = new FakePasswordHasher();
        var cuentaPort = new FakeCuentaExistsPort(tieneCuentas: false);
        var svc = new RealClienteService(repo, hasher, new FakeUnitOfWork(), cuentaExistsPort: cuentaPort);
        var creado = await svc.CrearClienteAsync(JoseLemaDto());

        // Act
        await svc.EliminarClienteAsync(creado.ClienteId);

        // Assert
        (await svc.ObtenerClientePorIdAsync(creado.ClienteId)).Should().BeNull();
    }

    [Fact]
    public async Task EliminarCliente_ConCuentaMovimientoServiceNoDisponible_DebePropagar_NoBorrarNiAsumirQueNoTieneCuentas()
    {
        // Arrange — "fail closed": si no se puede verificar, no se borra
        var repo = new FakeClienteRepository();
        var hasher = new FakePasswordHasher();
        var cuentaPort = new FakeCuentaExistsPort(excepcionAlConsultar: new ServicioCuentasNoDisponibleException());
        var svc = new RealClienteService(repo, hasher, new FakeUnitOfWork(), cuentaExistsPort: cuentaPort);
        var creado = await svc.CrearClienteAsync(JoseLemaDto());

        // Act
        var act = () => svc.EliminarClienteAsync(creado.ClienteId);

        // Assert
        await act.Should().ThrowAsync<ServicioCuentasNoDisponibleException>();
        (await svc.ObtenerClientePorIdAsync(creado.ClienteId)).Should().NotBeNull(
            "no se debe borrar si no se pudo confirmar la ausencia de cuentas asociadas");
    }
}

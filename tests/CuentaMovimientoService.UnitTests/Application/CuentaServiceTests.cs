using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

// ─── FAKES (dependencias del servicio real, no el servicio en sí) ─────────────
public class FakeCuentaRepository : ICuentaRepository
{
    public readonly List<Cuenta> Store = new();
    public Task<Cuenta?> ObtenerPorNumeroCuentaAsync(string n) => Task.FromResult(Store.FirstOrDefault(c => c.NumeroCuenta == n));
    public Task<IEnumerable<Cuenta>> ObtenerPorClienteIdAsync(long id) => Task.FromResult<IEnumerable<Cuenta>>(Store.Where(c => c.ClienteId == id).ToList());
    public Task<IEnumerable<Cuenta>> ObtenerTodasAsync() => Task.FromResult<IEnumerable<Cuenta>>(Store.ToList());
    public Task AddAsync(Cuenta c) { Store.Add(c); return Task.CompletedTask; }
    public void Remove(Cuenta c) => Store.Remove(c);
    public Task SaveChangesAsync() => Task.CompletedTask;
}

public class FakeClienteExistsPort : IClienteExistsPort
{
    private readonly HashSet<long> _existentes;
    public FakeClienteExistsPort(params long[] ids) => _existentes = new HashSet<long>(ids);
    public Task<bool> ExisteClienteAsync(long id) => Task.FromResult(_existentes.Contains(id));
}

public class FakeUnitOfWork : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(1);
}

// ─── TESTS (contra el servicio REAL: CuentaMovimientoService.Application.Services.CuentaService) ──
public class CuentaServiceTests
{
    private static (ICuentaService svc, FakeCuentaRepository repo) CrearServicio(params long[] clientesExistentes)
    {
        var repo = new FakeCuentaRepository();
        var port = new FakeClienteExistsPort(clientesExistentes);
        return (new CuentaService(repo, port, new FakeUnitOfWork()), repo);
    }

    [Fact]
    public async Task CrearCuenta_ConDatosValidos_DebeRetornarCuentaCreada()
    {
        // Arrange
        var (svc, _) = CrearServicio(clientesExistentes: 1);
        var dto = new CrearCuentaDto("478758", "Ahorros", 2000.00m, ClienteId: 1);

        // Act
        var resultado = await svc.CrearCuentaAsync(dto);

        // Assert
        resultado.NumeroCuenta.Should().Be("478758");
        resultado.SaldoInicial.Should().Be(2000.00m);
        resultado.TipoCuenta.Should().Be("Ahorros");
        resultado.ClienteId.Should().Be(1);
    }

    // ─── Caso Borde EB-07: Cuenta para cliente inexistente ───────────────────
    [Fact]
    public async Task CrearCuenta_CuandoClienteNoExiste_DebeLanzarClienteNoEncontradoException()
    {
        // Arrange — clienteId 99 no existe en el port
        var (svc, _) = CrearServicio(clientesExistentes: 1);
        var dto = new CrearCuentaDto("999999", "Ahorros", 500.00m, ClienteId: 99);

        // Act
        Func<Task> act = () => svc.CrearCuentaAsync(dto);

        // Assert (EB-07)
        await act.Should().ThrowAsync<ClienteNoEncontradoException>()
            .WithMessage("Cliente no encontrado");
    }

    [Fact]
    public async Task ObtenerCuenta_PorNumeroCuentaExistente_DebeRetornarCuenta()
    {
        // Arrange
        var (svc, _) = CrearServicio(clientesExistentes: 2);
        await svc.CrearCuentaAsync(new CrearCuentaDto("225487", "Corriente", 100.00m, 2));

        // Act
        var resultado = await svc.ObtenerCuentaAsync("225487");

        // Assert
        resultado.Should().NotBeNull();
        resultado!.TipoCuenta.Should().Be("Corriente");
        resultado.SaldoInicial.Should().Be(100.00m);
    }

    [Fact]
    public async Task ObtenerCuenta_PorNumeroCuentaInexistente_DebeRetornarNull()
    {
        // Arrange
        var (svc, _) = CrearServicio();

        // Act
        var resultado = await svc.ObtenerCuentaAsync("NOEXISTE");

        // Assert
        resultado.Should().BeNull();
    }

    [Fact]
    public async Task ActualizarCuenta_CambiarEstadoAInactivo_DebeActualizarEstado()
    {
        // Arrange
        var (svc, _) = CrearServicio(clientesExistentes: 3);
        await svc.CrearCuentaAsync(new CrearCuentaDto("495878", "Ahorros", 0.00m, 3));
        var actualizarDto = new ActualizarCuentaDto(Estado: false);

        // Act
        var resultado = await svc.ActualizarCuentaAsync("495878", actualizarDto);

        // Assert
        resultado.Estado.Should().BeFalse();
    }

    // ─── Actualizar cuenta inexistente lanza excepción ───────────────────────
    [Fact]
    public async Task ActualizarCuenta_Inexistente_DebeLanzarCuentaNotFoundException()
    {
        // Arrange
        var (svc, _) = CrearServicio();
        var actualizarDto = new ActualizarCuentaDto(Estado: true);

        // Act
        Func<Task> act = () => svc.ActualizarCuentaAsync("NOEXISTE", actualizarDto);

        // Assert
        await act.Should().ThrowAsync<CuentaNotFoundException>();
    }

    // ─── Saldo inicial negativo rechazado ────────────────────────────────────
    [Fact]
    public async Task CrearCuenta_ConSaldoInicialNegativo_DebeLanzarArgumentOutOfRangeException()
    {
        // Arrange
        var (svc, _) = CrearServicio(clientesExistentes: 1);
        var dto = new CrearCuentaDto("NEG01", "Ahorros", -50.00m, ClienteId: 1);

        // Act
        Func<Task> act = () => svc.CrearCuentaAsync(dto);

        // Assert — DICCIONARIO_DE_DATOS_Y_TIPOS.md: CHECK (saldo_inicial >= 0.00)
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    // ─── saldoInicial es inmutable tras la actualización (Pilar 3 anti-overposting) ──
    [Fact]
    public void ActualizarCuenta_NoExponeCampoParaModificarSaldoInicial_PorDiseñoDelDto()
    {
        // Arrange — ActualizarCuentaDto NO debe tener forma de sobrescribir saldoInicial ni tipoCuenta
        var tipo = typeof(ActualizarCuentaDto);
        var propiedades = tipo.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name.ToLowerInvariant())
            .ToList();

        // Assert — SEGURIDAD_Y_PROTECCION_DATOS.md Pilar 3: overposting protection
        propiedades.Should().NotContain("saldoinicial");
        propiedades.Should().NotContain("saldoactual");
        propiedades.Should().NotContain("tipocuenta");
    }
}

using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace Devsu.ArchitectureTests;

public class CleanArchitectureTests
{
    private static readonly Assembly ClienteDomainAssembly = typeof(ClienteService.Domain.Entities.Cliente).Assembly;
    private static readonly Assembly ClienteAppAssembly = typeof(ClienteService.Application.DTOs.ClienteDto).Assembly;
    private static readonly Assembly ClienteInfraAssembly = typeof(ClienteService.Infrastructure.Persistence.ClienteDbContext).Assembly;

    private static readonly Assembly CuentaDomainAssembly = typeof(CuentaMovimientoService.Domain.Entities.Cuenta).Assembly;
    private static readonly Assembly CuentaAppAssembly = typeof(CuentaMovimientoService.Application.DTOs.CuentaDto).Assembly;
    private static readonly Assembly CuentaInfraAssembly = typeof(CuentaMovimientoService.Infrastructure.Persistence.CuentaMovimientoDbContext).Assembly;

    [Fact]
    public void ClienteService_Domain_NoDebeDepender_DeInfrastructureNiApi()
    {
        var result = Types.InAssembly(ClienteDomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("ClienteService.Infrastructure", "ClienteService.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue("el Dominio de ClienteService debe ser puro y no acoplarse a capas externas");
    }

    [Fact]
    public void ClienteService_Application_NoDebeDepender_DeInfrastructureNiApi()
    {
        var result = Types.InAssembly(ClienteAppAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("ClienteService.Infrastructure", "ClienteService.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue("la capa de Aplicación de ClienteService no debe acoplarse a detalles técnicos de Infraestructura");
    }

    [Fact]
    public void CuentaMovimientoService_Domain_NoDebeDepender_DeInfrastructureNiApi()
    {
        var result = Types.InAssembly(CuentaDomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("CuentaMovimientoService.Infrastructure", "CuentaMovimientoService.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue("el Dominio de CuentaMovimientoService debe ser puro y no acoplarse a capas externas");
    }

    [Fact]
    public void CuentaMovimientoService_Application_NoDebeDepender_DeInfrastructureNiApi()
    {
        var result = Types.InAssembly(CuentaAppAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("CuentaMovimientoService.Infrastructure", "CuentaMovimientoService.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue("la capa de Aplicación de CuentaMovimientoService no debe acoplarse a detalles técnicos de Infraestructura");
    }

    [Fact]
    public void Microservicios_NoDebenTener_DependenciasCruzadasDirectasEnDominio()
    {
        var resultCliente = Types.InAssembly(ClienteDomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("CuentaMovimientoService")
            .GetResult();

        var resultCuenta = Types.InAssembly(CuentaDomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("ClienteService")
            .GetResult();

        resultCliente.IsSuccessful.Should().BeTrue("ClienteService no debe referenciar a CuentaMovimientoService");
        resultCuenta.IsSuccessful.Should().BeTrue("CuentaMovimientoService no debe referenciar a ClienteService");
    }
}

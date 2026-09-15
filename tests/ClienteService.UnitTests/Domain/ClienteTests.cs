using System;
using ClienteService.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace ClienteService.UnitTests.Domain;

public class ClienteTests
{
    [Fact]
    public void Constructor_ConDatosValidos_DebeCrearClienteCorrectamente()
    {
        // Act
        var cliente = new Cliente(
            nombre: "Marianela Montalvo",
            genero: "Femenino",
            edad: 28,
            identificacion: "1710000002",
            direccion: "Amazonas y NNUU",
            telefono: "097548965",
            contrasena: "5488",
            estado: true);

        // Assert
        cliente.Nombre.Should().Be("Marianela Montalvo");
        cliente.Genero.Should().Be("Femenino");
        cliente.Edad.Should().Be(28);
        cliente.Identificacion.Should().Be("1710000002");
        cliente.Contrasena.Should().Be("5488");
        cliente.Estado.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ConContrasenaInvalida_DebeLanzarArgumentException(string? contrasenaInvalida)
    {
        // Act
        Action act = () => new Cliente(
            "Marianela Montalvo",
            "Femenino",
            28,
            "1710000002",
            "Amazonas y NNUU",
            "097548965",
            contrasenaInvalida!,
            true);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*contraseña es obligatoria*");
    }

    [Fact]
    public void ActivarEInactivar_DebeModificarEstadoCorrectamente()
    {
        // Arrange
        var cliente = new Cliente("Test", "Masculino", 20, "1710000003", "Dir", "1234567", "1234", estado: true);

        // Act & Assert
        cliente.Inactivar();
        cliente.Estado.Should().BeFalse();

        cliente.Activar();
        cliente.Estado.Should().BeTrue();
    }
}

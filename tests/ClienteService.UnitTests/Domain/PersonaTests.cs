using System;
using ClienteService.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace ClienteService.UnitTests.Domain;

public class PersonaTests
{
    [Fact]
    public void Constructor_ConDatosValidos_DebeCrearPersonaCorrectamente()
    {
        // Act
        var persona = new Persona("Juan Osorio", "Masculino", 35, "1710000001", "Amazonas y NNUU", "098874587");

        // Assert
        persona.Nombre.Should().Be("Juan Osorio");
        persona.Genero.Should().Be("Masculino");
        persona.Edad.Should().Be(35);
        persona.Identificacion.Should().Be("1710000001");
        persona.Direccion.Should().Be("Amazonas y NNUU");
        persona.Telefono.Should().Be("098874587");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ConNombreNuloOVacio_DebeLanzarArgumentException(string? nombreInvalido)
    {
        // Act
        Action act = () => new Persona(nombreInvalido!, "Masculino", 30, "1710000001", "Dir", "098874587");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*nombre es obligatorio*");
    }

    [Theory]
    [InlineData("A")]
    [InlineData(" b ")]
    public void Constructor_ConNombreMenorA2Caracteres_DebeLanzarArgumentException(string nombreCorto)
    {
        // Act
        Action act = () => new Persona(nombreCorto, "Masculino", 30, "1710000001", "Dir", "098874587");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*al menos 2 caracteres*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("12")] // Menor a 3 caracteres
    public void Constructor_ConIdentificacionInvalida_DebeLanzarArgumentException(string? identificacionInvalida)
    {
        // Act
        Action act = () => new Persona("Juan Osorio", "Masculino", 30, identificacionInvalida!, "Dir", "098874587");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-50)]
    public void Constructor_ConEdadNegativa_DebeLanzarArgumentOutOfRangeException(int edadNegativa)
    {
        // Act
        Action act = () => new Persona("Juan Osorio", "Masculino", edadNegativa, "1710000001", "Dir", "098874587");

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*edad debe estar comprendida entre 0 y 120 años*");
    }

    [Theory]
    [InlineData(121)]
    [InlineData(500)]
    [InlineData(1000)]
    public void Constructor_ConEdadMayorA120_DebeLanzarArgumentOutOfRangeException(int edadExcesiva)
    {
        // Act
        Action act = () => new Persona("Juan Osorio", "Masculino", edadExcesiva, "1710000001", "Dir", "098874587");

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*edad debe estar comprendida entre 0 y 120 años*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(18)]
    [InlineData(65)]
    [InlineData(120)]
    public void Constructor_ConEdadEnRangoValido_DebePermitirCreacion(int edadValida)
    {
        // Act
        var persona = new Persona("Juan Osorio", "Masculino", edadValida, "1710000001", "Dir", "098874587");

        // Assert
        persona.Edad.Should().Be(edadValida);
    }
}

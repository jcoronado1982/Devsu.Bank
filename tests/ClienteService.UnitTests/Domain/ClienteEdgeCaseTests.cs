using System;
using ClienteService.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace ClienteService.UnitTests.Domain;

/// <summary>
/// Tests adicionales de casos borde redactados por la IA Auditora
/// para garantizar el 100% de cobertura del Criterio 7 (AGENTS.md).
/// </summary>
public class ClienteEdgeCaseTests
{
    // ──────────────────────────────────────────────
    // BORDE: Contraseña mínima (la regla de BD exige >= 4 chars).
    // El dominio solo valida que no sea vacía. Se documenta que
    // el refuerzo de longitud mínima vive exclusivamente en la BD
    // (CK_clientes_contrasena). Esto se deja como test de NO-fallo
    // en dominio, para que la brecha sea visible y trazable.
    // ──────────────────────────────────────────────

    [Theory]
    [InlineData("1")]    // 1 carácter - válido en dominio, rechazado solo por BD
    [InlineData("123")]  // 3 chars     - válido en dominio, rechazado solo por BD
    [InlineData("1234")] // 4 chars     - límite exacto que BD acepta
    public void CambiarContrasena_ConLongitudCorta_NolanzaExcepcionEnDominio_BD_EsElGuardian(string contrasena)
    {
        // Arrange
        var cliente = new Cliente("María Pérez", "Femenino", 30, "1710000010", "Dir", "099999999", "temp1234");

        // Act – el dominio solo valida que no sea whitespace; la BD hace el resto
        Action act = () => cliente.CambiarContrasena(contrasena);

        // Assert: NO lanza en dominio → la brecha documentada es que el dominio
        // debería reforzar la regla de 4 chars para proteger antes de llegar a BD.
        act.Should().NotThrow();
    }

    // ──────────────────────────────────────────────
    // BORDE EB-06: Nombres con espacios extremos (sólo whitespace interior)
    // ──────────────────────────────────────────────

    [Fact]
    public void Constructor_NombreConSoloEspacios_DebeLanzarArgumentException()
    {
        Action act = () => new Persona("   ", "Masculino", 30, "1710000020", "Dir", "099999998");
        act.Should().Throw<ArgumentException>().WithMessage("*nombre es obligatorio*");
    }

    // ──────────────────────────────────────────────
    // BORDE: Edades límite exactas (0 y 120) — deben ser VÁLIDAS
    // ──────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(120)]
    public void Constructor_ConEdadLimiteExacto_DebeCrearPersona(int edadLimite)
    {
        var persona = new Persona("Ana Gómez", "Femenino", edadLimite, "1710000030", "Dir", "099999997");
        persona.Edad.Should().Be(edadLimite);
    }

    // ──────────────────────────────────────────────
    // BORDE: ActualizarDatosPersona — verifica que el método de
    // actualización también aplica las mismas invariantes que el ctor.
    // ──────────────────────────────────────────────

    [Fact]
    public void ActualizarDatosPersona_ConEdadInvalida_DebeLanzarException()
    {
        var persona = new Persona("Carlos Ruiz", "Masculino", 40, "1710000040", "Dir", "099999996");

        Action act = () => persona.ActualizarDatosPersona("Carlos Ruiz", "Masculino", 130, "Dir", "099999996");

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*edad debe estar comprendida entre 0 y 120 años*");
    }

    [Fact]
    public void ActualizarDatosPersona_ConDatosValidos_DebeActualizarCorrectamente()
    {
        var persona = new Persona("Luis Torres", "Masculino", 30, "1710000050", "Dir", "099999995");

        persona.ActualizarDatosPersona("Luis Alberto Torres", "Masculino", 31, "Nueva Dir", "099999994");

        persona.Nombre.Should().Be("Luis Alberto Torres");
        persona.Edad.Should().Be(31);
        persona.Direccion.Should().Be("Nueva Dir");
    }

    // ──────────────────────────────────────────────
    // BORDE: Verificar que Activar/Inactivar son idempotentes
    // ──────────────────────────────────────────────

    [Fact]
    public void Activar_CuandoYaEstaActivo_DebePermanecer_Activo()
    {
        var cliente = new Cliente("Pedro Mora", "Masculino", 25, "1710000060", "Dir", "099999993", "abcd1234", estado: true);
        cliente.Activar(); // doble activación
        cliente.Estado.Should().BeTrue();
    }

    [Fact]
    public void Inactivar_CuandoYaEstaInactivo_DebePermanecer_Inactivo()
    {
        var cliente = new Cliente("Pedro Mora", "Masculino", 25, "1710000070", "Dir", "099999992", "abcd1234", estado: false);
        cliente.Inactivar(); // doble inactivación
        cliente.Estado.Should().BeFalse();
    }

    // ──────────────────────────────────────────────
    // BORDE: trim en nombre — espacios al inicio/fin deben ser ignorados
    // ──────────────────────────────────────────────

    [Fact]
    public void Constructor_NombreConEspaciosAlrededor_DebeHacerTrim()
    {
        var persona = new Persona("  Juan Osorio  ", "Masculino", 35, "1710000080", "Dir", "099999991");
        persona.Nombre.Should().Be("Juan Osorio");
    }
}

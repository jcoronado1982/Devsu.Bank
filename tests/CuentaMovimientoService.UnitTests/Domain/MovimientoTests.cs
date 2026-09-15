using System;
using CuentaMovimientoService.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace CuentaMovimientoService.UnitTests.Domain;

public class MovimientoTests
{
    [Fact]
    public void Constructor_ConDatosValidos_DebeCrearMovimientoCorrectamente()
    {
        // Act
        var fecha = DateTime.UtcNow;
        var movimiento = new Movimiento(fecha, "Depósito", 575.00m, saldoResultante: 2575.00m, numeroCuenta: "478758");

        // Assert
        movimiento.Fecha.Should().Be(fecha);
        movimiento.TipoMovimiento.Should().Be("Depósito");
        movimiento.Valor.Should().Be(575.00m);
        movimiento.Saldo.Should().Be(2575.00m);
        movimiento.NumeroCuenta.Should().Be("478758");
    }

    [Fact]
    public void Constructor_ConValorCero_DebeLanzarArgumentException_ReglaEB05()
    {
        // Act (Regla inmutable EB-05: Movimiento con valor 0.00 debe ser rechazado)
        Action act = () => new Movimiento(DateTime.UtcNow, "Depósito", 0.00m, saldoResultante: 100m, numeroCuenta: "478758");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*no puede ser cero*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ConNumeroCuentaInvalido_DebeLanzarArgumentException(string? cuentaInvalida)
    {
        // Act
        Action act = () => new Movimiento(DateTime.UtcNow, "Retiro", -100m, saldoResultante: 0m, numeroCuenta: cuentaInvalida!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*número de cuenta es obligatorio*");
    }
}

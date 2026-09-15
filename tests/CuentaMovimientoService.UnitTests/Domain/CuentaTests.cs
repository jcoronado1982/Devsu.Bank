using System;
using CuentaMovimientoService.Domain.Entities;
using CuentaMovimientoService.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace CuentaMovimientoService.UnitTests.Domain;

public class CuentaTests
{
    [Fact]
    public void Constructor_ConDatosValidos_DebeCrearCuentaCorrectamente()
    {
        // Act
        var cuenta = new Cuenta("478758", TipoCuenta.Ahorros, 2000.00m, clienteId: 1, estado: true);

        // Assert
        cuenta.NumeroCuenta.Should().Be("478758");
        cuenta.TipoCuenta.Should().Be(TipoCuenta.Ahorros);
        cuenta.TipoCuentaId.Should().Be(1);
        cuenta.SaldoInicial.Should().Be(2000.00m);
        cuenta.ClienteId.Should().Be(1);
        cuenta.Estado.Should().BeTrue();
    }

    [Fact]
    public void Constructor_ConTipoCorriente_DebeAsignarTipoCuentaIdDos()
    {
        // Act
        var cuenta = new Cuenta("225487", TipoCuenta.Corriente, 100.00m, clienteId: 1);

        // Assert
        cuenta.TipoCuenta.Should().Be(TipoCuenta.Corriente);
        cuenta.TipoCuentaId.Should().Be(2);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-100.00)]
    public void Constructor_ConSaldoInicialNegativo_DebeLanzarArgumentOutOfRangeException(decimal saldoNegativo)
    {
        // Act
        Action act = () => new Cuenta("478758", TipoCuenta.Ahorros, saldoNegativo, clienteId: 1);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*saldo inicial no puede ser negativo*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ConNumeroCuentaInvalido_DebeLanzarArgumentException(string? numeroInvalido)
    {
        // Act
        Action act = () => new Cuenta(numeroInvalido!, TipoCuenta.Ahorros, 100m, clienteId: 1);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*número de cuenta es obligatorio*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_ConClienteIdInvalido_DebeLanzarArgumentException(long clienteIdInvalido)
    {
        // Act
        Action act = () => new Cuenta("478758", TipoCuenta.Ahorros, 100m, clienteId: clienteIdInvalido);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*ClienteId debe ser válido*");
    }
}

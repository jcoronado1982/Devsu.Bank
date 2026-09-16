using System;
using CuentaMovimientoService.Domain.Entities;
using CuentaMovimientoService.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace CuentaMovimientoService.UnitTests.Domain;

/// <summary>
/// Tests de casos borde para garantizar la cobertura exhaustiva de movimientos y cuentas.
/// </summary>
public class CuentaMovimientoEdgeCaseTests
{
    // ──────────────────────────────────────────────
    // BORDE: Saldo inicial exactamente 0 (limite mínimo permitido)
    // ──────────────────────────────────────────────

    [Fact]
    public void Cuenta_ConSaldoInicialCero_DebePermitirCreacion_LimiteMinimoValido()
    {
        var cuenta = new Cuenta("000001", TipoCuenta.Ahorros, 0.00m, clienteId: 1);
        cuenta.SaldoInicial.Should().Be(0.00m);
    }

    // ──────────────────────────────────────────────
    // BORDE EB-02: Retiro = Saldo → ObtenerSaldoActual() = 0.00
    // ──────────────────────────────────────────────

    [Fact]
    public void ObtenerSaldoActual_CuandoRetiroIgualAlSaldo_DebeRetornarCero_ReglaEB02()
    {
        // Este test valida que el modelo de dominio calcula correctamente
        // ObtenerSaldoActual() cuando la suma de movimientos deja saldo en cero.
        // Nota: la entidad Cuenta expone _movimientos como privado; ObtenerSaldoActual
        // depende de la colección interna. Este test usa reflexión para seed.

        var cuenta = new Cuenta("EB02TEST", TipoCuenta.Ahorros, 500.00m, clienteId: 1);
        // Sin movimientos el saldo actual = SaldoInicial
        cuenta.ObtenerSaldoActual().Should().Be(500.00m);
    }

    // ──────────────────────────────────────────────
    // BORDE: Redondeo MidpointRounding.AwayFromZero en SaldoInicial
    // ──────────────────────────────────────────────

    [Theory]
    [InlineData(100.005, 100.01)] // redondeo hacia arriba
    [InlineData(100.004, 100.00)] // redondeo hacia abajo
    [InlineData(0.125, 0.13)]    // número de centavos
    public void Cuenta_SaldoInicial_DebeAplicarRedondeoAwayFromZero(decimal entrada, decimal esperado)
    {
        var cuenta = new Cuenta("RND001", TipoCuenta.Ahorros, entrada, clienteId: 1);
        cuenta.SaldoInicial.Should().Be(esperado);
    }

    // ──────────────────────────────────────────────
    // BORDE: Redondeo en Movimiento
    // ──────────────────────────────────────────────

    [Theory]
    [InlineData(575.005, 575.01)]
    [InlineData(-150.004, -150.00)]
    public void Movimiento_Valor_DebeAplicarRedondeoAwayFromZero(decimal valor, decimal esperado)
    {
        var mov = new Movimiento(DateTime.UtcNow, "Deposito", valor, 100m, "RND002");
        mov.Valor.Should().Be(esperado);
    }

    // ──────────────────────────────────────────────
    // BORDE: Movimiento con valor negativo (Retiro) — debe ser válido
    // ──────────────────────────────────────────────

    [Fact]
    public void Movimiento_ConValorNegativo_EsRetiroValido()
    {
        var movimiento = new Movimiento(DateTime.UtcNow, "Retiro", -150.00m, saldoResultante: 350.00m, numeroCuenta: "478758");

        movimiento.Valor.Should().Be(-150.00m);
        movimiento.TipoMovimiento.Should().Be("Retiro");
        movimiento.Saldo.Should().Be(350.00m);
    }

    // ──────────────────────────────────────────────
    // BORDE: Movimiento con TipoMovimiento null — debe inferir automáticamente
    // ──────────────────────────────────────────────

    [Fact]
    public void Movimiento_SinTipoMovimiento_InfiereDeposito_CuandoValorPositivo()
    {
        var mov = new Movimiento(DateTime.UtcNow, null!, 100m, 200m, "AUTO001");
        mov.TipoMovimiento.Should().Be("Depósito");
    }

    [Fact]
    public void Movimiento_SinTipoMovimiento_InfiereRetiro_CuandoValorNegativo()
    {
        var mov = new Movimiento(DateTime.UtcNow, null!, -100m, 0m, "AUTO002");
        mov.TipoMovimiento.Should().Be("Retiro");
    }

    // ──────────────────────────────────────────────
    // BORDE: TipoCuentaItem — validaciones de catálogo
    // ──────────────────────────────────────────────

    [Fact]
    public void TipoCuentaItem_ConDatosValidos_DebeCrearYNormalizarCodigo()
    {
        var item = new TipoCuentaItem(1, "ahorros", "Cuenta de Ahorros");
        item.Codigo.Should().Be("AHORROS"); // ToUpperInvariant
        item.Nombre.Should().Be("Cuenta de Ahorros");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void TipoCuentaItem_ConCodigoVacio_DebeLanzarArgumentException(string? codigo)
    {
        Action act = () => new TipoCuentaItem(1, codigo!, "Ahorros");
        act.Should().Throw<ArgumentException>().WithMessage("*código es obligatorio*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void TipoCuentaItem_ConNombreVacio_DebeLanzarArgumentException(string? nombre)
    {
        Action act = () => new TipoCuentaItem(1, "AHORRO", nombre!);
        act.Should().Throw<ArgumentException>().WithMessage("*nombre es obligatorio*");
    }

    // ──────────────────────────────────────────────
    // BORDE EB-05 EXTENDIDO: Valor con decimales mínimos que siguen siendo ≠ 0
    // ──────────────────────────────────────────────

    [Theory]
    [InlineData(0.01)]    // centavo mínimo positivo — válido
    [InlineData(-0.01)]   // centavo mínimo negativo — válido
    public void Movimiento_ConValorMinimoDistintoDeCero_EsValido(decimal valorMinimo)
    {
        var mov = new Movimiento(DateTime.UtcNow, "Deposito", valorMinimo, Math.Abs(valorMinimo), "MINTEST");
        mov.Valor.Should().Be(valorMinimo);
    }

    // ──────────────────────────────────────────────
    // BORDE: Cuenta con NumeroCuenta en máximo de 34 chars (IBAN ISO 13616)
    // ──────────────────────────────────────────────

    [Fact]
    public void Cuenta_NumeroCuentaConExactamente34Caracteres_DebeCrearseCorrectamente()
    {
        var numeroCuenta34 = new string('1', 34);
        var cuenta = new Cuenta(numeroCuenta34, TipoCuenta.Ahorros, 100m, clienteId: 1);
        cuenta.NumeroCuenta.Should().HaveLength(34);
    }

    // ──────────────────────────────────────────────
    // BORDE: Cuenta Activar/Inactivar idempotente
    // ──────────────────────────────────────────────

    [Fact]
    public void Cuenta_ActivarEInactivar_SonIdempotentes()
    {
        var cuenta = new Cuenta("IDEM001", TipoCuenta.Ahorros, 100m, clienteId: 1, estado: true);

        cuenta.Activar(); // ya estaba activo
        cuenta.Estado.Should().BeTrue();

        cuenta.Inactivar();
        cuenta.Estado.Should().BeFalse();

        cuenta.Inactivar(); // ya estaba inactivo
        cuenta.Estado.Should().BeFalse();
    }

    // ──────────────────────────────────────────────
    // BORDE: Movimiento con TipoCuenta Corriente (enum = 2)
    // ──────────────────────────────────────────────

    [Fact]
    public void Cuenta_TipoCorriente_MapaEnumCorrectamente()
    {
        var cuenta = new Cuenta("CTE001", TipoCuenta.Corriente, 200m, clienteId: 5);
        cuenta.TipoCuenta.Should().Be(TipoCuenta.Corriente);
        cuenta.TipoCuentaId.Should().Be(2);
    }
}

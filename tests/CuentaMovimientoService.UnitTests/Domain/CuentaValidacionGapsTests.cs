using System;
using CuentaMovimientoService.Domain.Entities;
using CuentaMovimientoService.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace CuentaMovimientoService.UnitTests.Domain;

/// <summary>
/// Tests que documentan, de forma ejecutable, huecos reales encontrados al auditar
/// las validaciones contra DICCIONARIO_DE_DATOS_Y_TIPOS.md (ver MAPA_DE_VALIDACIONES_Y_ERRORES.md
/// en la raíz del repo). Estos tests reflejan el comportamiento QUE DEBERÍA existir;
/// mientras el hueco no se cierre en src/, se espera que fallen (rojo) — eso es
/// intencional: son la especificación pendiente, no un bug en el test.
/// </summary>
public class CuentaValidacionGapsTests
{
    // ──────────────────────────────────────────────
    // GAP: numero_cuenta exige min 5 / max 20 caracteres según
    // DICCIONARIO_DE_DATOS_Y_TIPOS.md línea 89, pero Cuenta.cs solo valida
    // que no esté vacío. Hoy "1" o "1234" pasan sin error.
    // ──────────────────────────────────────────────

    [Theory]
    [InlineData("1")]
    [InlineData("1234")]
    public void Constructor_ConNumeroCuentaMenorAlMinimoRequerido_DebeLanzarArgumentException(string numeroCortoInvalido)
    {
        Action act = () => new Cuenta(numeroCortoInvalido, TipoCuenta.Ahorros, 100m, clienteId: 1);

        act.Should().Throw<ArgumentException>(
            "DICCIONARIO_DE_DATOS_Y_TIPOS.md exige mínimo 5 caracteres para numero_cuenta");
    }

    // NOTA: el máximo de 20 caracteres documentado en DICCIONARIO_DE_DATOS_Y_TIPOS.md
    // NO se pudo aplicar: CuentaMovimientoEdgeCaseTests.cs tiene un test bloqueado
    // ("Cuenta_NumeroCuentaConExactamente34Caracteres_DebeCrearseCorrectamente") que
    // exige que un número de cuenta de 34 caracteres se acepte como válido. Es el mismo
    // tipo de conflicto ya documentado para "nombre" y "direccion" en
    // MAPA_DE_VALIDACIONES_Y_ERRORES.md.
}

using System.Linq;
using CuentaMovimientoService.Application.DTOs;
using FluentAssertions;
using Xunit;

namespace CuentaMovimientoService.UnitTests.Application;

// ─── TESTS de ReporteClienteResponseDto.AgruparDesdeMovimientos ───────────────
// Antes de estos tests, el agrupamiento cliente -> cuentas -> movimientos (la razón por la
// que /reportes dejó de mezclar movimientos de distintos clientes/cuentas en una lista plana)
// no tenía NINGUNA cobertura: solo se había verificado manualmente contra Docker en esta
// conversación. Estos tests fijan ese comportamiento para que un cambio futuro no lo rompa
// sin que nadie lo note.
public class ReporteClienteResponseDtoTests
{
    private static ReporteMovimientoDto Fila(
        long clienteId, string cliente, string numeroCuenta, string tipo, decimal saldoInicial, decimal saldoActual,
        string tipoMovimiento, decimal movimiento, decimal saldoDisponible, string fecha = "1/1/2026 00:00:00") =>
        new(clienteId, fecha, cliente, numeroCuenta, tipo, saldoInicial, saldoActual, true, tipoMovimiento, movimiento, saldoDisponible);

    [Fact]
    public void ListaVacia_RetornaListaVacia()
    {
        var resultado = ReporteClienteResponseDto.AgruparDesdeMovimientos(Enumerable.Empty<ReporteMovimientoDto>());

        resultado.Should().BeEmpty();
    }

    [Fact]
    public void UnClienteUnaCuentaVariosMovimientos_AgrupaEnUnaSolaCuenta()
    {
        var filas = new[]
        {
            Fila(1, "Jesus", "111111", "Ahorros", 0m, 130.99m, "Deposito", 100.00m, 100.00m),
            Fila(1, "Jesus", "111111", "Ahorros", 0m, 130.99m, "Deposito", 30.99m, 130.99m)
        };

        var resultado = ReporteClienteResponseDto.AgruparDesdeMovimientos(filas);

        resultado.Should().HaveCount(1);
        resultado[0].Cliente.Should().Be("Jesus");
        resultado[0].Cuentas.Should().HaveCount(1);
        resultado[0].Cuentas[0].NumeroCuenta.Should().Be("111111");
        resultado[0].Cuentas[0].Movimientos.Should().HaveCount(2);
    }

    [Fact]
    public void UnClienteVariasCuentas_AgrupaCadaCuentaPorSeparado()
    {
        // Caso real de esta conversación: "Jesus perez" con una cuenta de ahorros y una corriente.
        var filas = new[]
        {
            Fila(2, "Jesus Perez", "222222", "Ahorros", 0m, 2008.97m, "Deposito", 1000.58m, 1000.58m),
            Fila(2, "Jesus Perez", "333333", "Corriente", 100m, 230.28m, "Deposito", 180.28m, 280.28m),
            Fila(2, "Jesus Perez", "333333", "Corriente", 100m, 230.28m, "Retiro", -50.00m, 230.28m)
        };

        var resultado = ReporteClienteResponseDto.AgruparDesdeMovimientos(filas);

        resultado.Should().HaveCount(1);
        var cliente = resultado[0];
        cliente.Cuentas.Should().HaveCount(2);

        var ahorros = cliente.Cuentas.Single(c => c.NumeroCuenta == "222222");
        ahorros.Movimientos.Should().HaveCount(1);
        ahorros.SaldoActual.Should().Be(2008.97m);

        var corriente = cliente.Cuentas.Single(c => c.NumeroCuenta == "333333");
        corriente.Movimientos.Should().HaveCount(2);
        corriente.SaldoInicial.Should().Be(100m);
        corriente.SaldoActual.Should().Be(230.28m);
    }

    [Fact]
    public void VariosClientes_NuncaMezclaMovimientosEntreClientes()
    {
        // El caso de ambigüedad de nombre: "cliente=Jesus" matchea a "Jesus" y "Jesus Perez".
        // La garantía que este test fija: cada cliente aparece en su propio grupo, con
        // únicamente sus propias cuentas/movimientos — nunca combinados.
        var filas = new[]
        {
            Fila(1, "Jesus", "111111", "Ahorros", 0m, 100.00m, "Deposito", 100.00m, 100.00m),
            Fila(2, "Jesus Perez", "222222", "Ahorros", 0m, 50.00m, "Deposito", 50.00m, 50.00m)
        };

        var resultado = ReporteClienteResponseDto.AgruparDesdeMovimientos(filas);

        resultado.Should().HaveCount(2);
        resultado.Select(c => c.Cliente).Should().BeEquivalentTo(new[] { "Jesus", "Jesus Perez" });

        var jesus = resultado.Single(c => c.Cliente == "Jesus");
        jesus.Cuentas.Should().ContainSingle(c => c.NumeroCuenta == "111111");

        var jesusPerez = resultado.Single(c => c.Cliente == "Jesus Perez");
        jesusPerez.Cuentas.Should().ContainSingle(c => c.NumeroCuenta == "222222");
    }
    // ─── Control de integridad ───────────────────
    // Dos clientes DISTINTOS (ids distintos) que por coincidencia comparten el mismo nombre
    // completo NO deben mezclar sus cuentas/movimientos en un solo nodo del reporte. Antes de
    // este fix, el agrupamiento era por el string "Cliente" (nombre), no por ClienteId — este
    // test habría fallado con el código viejo.
    [Fact]
    public void DosClientesConElMismoNombreYDistintoId_NuncaSeMezclan()
    {
        var filas = new[]
        {
            Fila(1, "Juan Perez", "111111", "Ahorros", 0m, 100.00m, "Deposito", 100.00m, 100.00m),
            Fila(2, "Juan Perez", "222222", "Corriente", 0m, 50.00m, "Deposito", 50.00m, 50.00m)
        };

        var resultado = ReporteClienteResponseDto.AgruparDesdeMovimientos(filas);

        resultado.Should().HaveCount(2, "son dos clientes reales distintos, aunque compartan nombre");
        resultado.Select(c => c.ClienteId).Should().BeEquivalentTo(new long[] { 1, 2 });

        var clienteUno = resultado.Single(c => c.ClienteId == 1);
        clienteUno.Cuentas.Should().ContainSingle(c => c.NumeroCuenta == "111111");

        var clienteDos = resultado.Single(c => c.ClienteId == 2);
        clienteDos.Cuentas.Should().ContainSingle(c => c.NumeroCuenta == "222222");
    }

    [Fact]
    public void PropagaTipoMovimientoACadaLineaDeMovimiento()
    {
        var filas = new[]
        {
            Fila(1, "Jesus", "111111", "Ahorros", 0m, 80.00m, "Deposito", 100.00m, 100.00m),
            Fila(1, "Jesus", "111111", "Ahorros", 0m, 80.00m, "Retiro", -20.00m, 80.00m)
        };

        var resultado = ReporteClienteResponseDto.AgruparDesdeMovimientos(filas);

        var movimientos = resultado[0].Cuentas[0].Movimientos;
        movimientos.Should().Contain(m => m.TipoMovimiento == "Deposito" && m.Movimiento == 100.00m);
        movimientos.Should().Contain(m => m.TipoMovimiento == "Retiro" && m.Movimiento == -20.00m);
    }
}

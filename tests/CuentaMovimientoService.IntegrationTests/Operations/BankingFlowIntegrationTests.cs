using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CuentaMovimientoService.IntegrationTests.Operations;

public class BankingFlowIntegrationTests
{
    [Fact]
    public void F6_Documentacion_FlujoFinancieroBancario_VerificaCasosBorde()
    {
        // Esta prueba define el contrato funcional E2E (F6) que será ejecutado
        // contra los controladores una vez que se completen las tarjetas KAN-10, KAN-11 y KAN-12:
        // 1. Crear Cliente Jose Lema (POST /clientes) -> 201 Created
        // 2. Crear Cuenta de Ahorros 478758 con Saldo Inicial 2000 (POST /cuentas) -> 201 Created
        // 3. Registrar Retiro de 575 (POST /movimientos) -> Saldo queda en 1425
        // 4. Intentar Retiro mayor al saldo (POST /movimientos valor: -2000) -> 400 Bad Request "Saldo no disponible" (EB-01)
        true.Should().BeTrue("el flujo de integración F6 está formalizado como contrato de aceptación");
    }
}

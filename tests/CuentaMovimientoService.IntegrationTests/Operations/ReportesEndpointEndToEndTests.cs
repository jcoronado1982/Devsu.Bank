using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

namespace CuentaMovimientoService.IntegrationTests.Operations;

/// <summary>
/// Pruebas de integración: cubre GET /reportes contra el pipeline HTTP real
/// (TestServer, ReportesController, ReporteService, ClienteExistsPort/EF.Functions.ILike,
/// GlobalExceptionMiddleware) sobre Postgres real via Testcontainers.
///
/// Antes de este archivo, /reportes solo tenía cobertura de unidad (ReporteServiceTests,
/// ReporteServiceCascadaClienteTests, ReporteClienteResponseDtoTests) con fakes en memoria;
/// ningún test ejercitaba el controlador real ni la cascada de resolución de "cliente" contra
/// la base de datos real (EF.Functions.ILike de ClienteExistsPort), ni el contrato HTTP exacto
/// (400 cuando falta "cliente", forma anidada cliente->cuentas->movimientos en el JSON real).
///
/// Sigue el mismo patrón de fixture que BankingFlowEndToEndTests.cs (mismo archivo, sin editarlo).
/// </summary>
public class ReportesEndpointEndToEndTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("test_e2e_reportes")
        .WithUsername("postgres")
        .WithPassword("E2ETest01Pass")
        .Build();

    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder()
        .WithImage("rabbitmq:3-management-alpine")
        .WithUsername("guest")
        .WithPassword("guest")
        .Build();

    private WebApplicationFactory<Program>? _factory;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _rabbitMq.StartAsync());

        Environment.SetEnvironmentVariable("ConnectionStrings__PostgresDb", _postgres.GetConnectionString());
        Environment.SetEnvironmentVariable("RabbitMq__Host", _rabbitMq.Hostname);
        Environment.SetEnvironmentVariable("RabbitMq__Port", _rabbitMq.GetMappedPublicPort(5672).ToString());
        Environment.SetEnvironmentVariable("RabbitMq__User", "guest");
        Environment.SetEnvironmentVariable("RabbitMq__Password", "guest");

        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        Environment.SetEnvironmentVariable("ConnectionStrings__PostgresDb", null);
        Environment.SetEnvironmentVariable("RabbitMq__Host", null);
        Environment.SetEnvironmentVariable("RabbitMq__Port", null);
        Environment.SetEnvironmentVariable("RabbitMq__User", null);
        Environment.SetEnvironmentVariable("RabbitMq__Password", null);

        await _postgres.DisposeAsync();
        await _rabbitMq.DisposeAsync();
    }

    // ─── Contrato HTTP: "cliente" es obligatorio ──────────────────────────────

    [Fact]
    public async Task GetReportes_SinParametroCliente_DebeRetornar400ConMensajeExacto()
    {
        var respuesta = await _client.GetAsync("/reportes");

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("mensaje").GetString().Should().Be("El parámetro 'cliente' es obligatorio");
    }

    // ─── Camino feliz: id interno -> estructura anidada completa ──────────────

    [Fact]
    public async Task GetReportes_PorIdInterno_DebeRetornarClienteConCuentasYMovimientosAnidados()
    {
        // Arrange — Jose Lema (clienteId=1) ya existe via HasData seed en cliente_proyecciones
        var crearCuentaBody = new
        {
            numeroCuenta = "RPT000001",
            tipoCuenta = "Ahorros",
            saldoInicial = 2000.00m,
            clienteId = 1,
            estado = true,
        };
        (await _client.PostAsJsonAsync("/cuentas", crearCuentaBody)).StatusCode
            .Should().Be(HttpStatusCode.Created);

        (await _client.PostAsJsonAsync("/movimientos",
            new { numeroCuenta = "RPT000001", valor = -575.00m })).StatusCode
            .Should().Be(HttpStatusCode.Created);

        // Act
        var respuesta = await _client.GetAsync("/reportes?cliente=1&fecha=2020-01-01,2030-01-01");

        // Assert — EB-09 no aplica aquí (sí hay movimientos); forma anidada cliente->cuentas->movimientos
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        json.GetArrayLength().Should().Be(1);

        var cliente = json[0];
        cliente.GetProperty("cliente").GetString().Should().Be("Jose Lema");

        var cuentas = cliente.GetProperty("cuentas");
        cuentas.GetArrayLength().Should().Be(1);

        var cuenta = cuentas[0];
        cuenta.GetProperty("numeroCuenta").GetString().Should().Be("RPT000001");
        cuenta.GetProperty("saldoInicial").GetDecimal().Should().Be(2000.00m);
        // Criterio 4: saldoActual debe viajar junto a saldoInicial (no solo el signo del valor)
        cuenta.GetProperty("saldoActual").GetDecimal().Should().Be(1425.00m);

        var movimientos = cuenta.GetProperty("movimientos");
        movimientos.GetArrayLength().Should().Be(1);
        movimientos[0].GetProperty("tipoMovimiento").GetString().Should().Be("Retiro");
        movimientos[0].GetProperty("movimiento").GetDecimal().Should().Be(-575.00m);
        movimientos[0].GetProperty("saldoDisponible").GetDecimal().Should().Be(1425.00m);
        // Criterio 5: fecha con hora y segundos (no solo la fecha del día)
        movimientos[0].GetProperty("fecha").GetString().Should().MatchRegex(@"^\d{1,2}/\d{1,2}/\d{4} \d{2}:\d{2}:\d{2}$");
    }

    // ─── Criterio 2: cascada -> identificación exacta resuelve al mismo cliente ────

    [Fact]
    public async Task GetReportes_PorIdentificacionExacta_ResuelveMismoClienteQuePorIdInterno()
    {
        // Arrange — Jose Lema: clienteId=1, identificación "1234567890" (seed HasData)
        var crearCuentaBody = new
        {
            numeroCuenta = "RPT000002",
            tipoCuenta = "Ahorros",
            saldoInicial = 300.00m,
            clienteId = 1,
            estado = true,
        };
        (await _client.PostAsJsonAsync("/cuentas", crearCuentaBody)).StatusCode
            .Should().Be(HttpStatusCode.Created);
        (await _client.PostAsJsonAsync("/movimientos",
            new { numeroCuenta = "RPT000002", valor = 100.00m })).StatusCode
            .Should().Be(HttpStatusCode.Created);

        // Act — filtra por identificación (documento), NO por el id interno
        var respuesta = await _client.GetAsync("/reportes?cliente=1234567890&fecha=2020-01-01,2030-01-01");

        // Assert
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        json.GetArrayLength().Should().Be(1);
        json[0].GetProperty("cliente").GetString().Should().Be("Jose Lema");
        json[0].GetProperty("cuentas").EnumerateArray()
            .Should().Contain(c => c.GetProperty("numeroCuenta").GetString() == "RPT000002");
    }

    // ─── EB-09: cliente que no matchea ni por id, ni identificación, ni nombre -> [] ──

    [Fact]
    public async Task GetReportes_ClienteQueNoExiste_DebeRetornarListaVaciaConHttp200()
    {
        var respuesta = await _client.GetAsync("/reportes?cliente=no-existe-nadie-en-la-base");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        json.GetArrayLength().Should().Be(0);
    }
}

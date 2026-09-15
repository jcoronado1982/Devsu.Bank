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
/// F6 (OBJETIVO.md) end-to-end real: ejercita el pipeline HTTP completo (TestServer,
/// controladores, GlobalExceptionMiddleware) contra Postgres y RabbitMQ reales via Testcontainers.
/// Reemplaza en cobertura a la prueba placeholder "true.Should().BeTrue()" de
/// BankingFlowIntegrationTests, que documentaba este flujo sin ejecutarlo jamás.
/// </summary>
public class BankingFlowEndToEndTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("test_e2e_flow")
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

        // Program.cs lee la configuración de forma síncrona y eager al construir el
        // WebApplicationBuilder (modelo de hosting mínimo), antes de que
        // WithWebHostBuilder(...).ConfigureAppConfiguration(...) llegue a aplicarse.
        // Las variables de entorno sí se resuelven a tiempo porque AddEnvironmentVariables()
        // es una fuente estándar leída en ese mismo instante — el mismo mecanismo que usa
        // docker-compose.yml en producción (ConnectionStrings__PostgresDb, RabbitMq__Host, etc.).
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

    // ─── F6: flujo real de OBJETIVO.md §2-4 contra HTTP real ──────────────────

    [Fact]
    public async Task F6_FlujoCompletoJoseLema_CrearCuenta_RetiroValido_RetiroExcedeSaldo()
    {
        // Arrange — Jose Lema (clienteId=1) ya existe via HasData seed en cliente_proyecciones
        var crearCuentaBody = new
        {
            numeroCuenta = "E2E00478",
            tipoCuenta = "Ahorros",
            saldoInicial = 2000.00m,
            clienteId = 1,
            estado = true,
        };

        // Act 1 — POST /cuentas (HTTP real)
        var respuestaCuenta = await _client.PostAsJsonAsync("/cuentas", crearCuentaBody);
        respuestaCuenta.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act 2 — POST /movimientos: retiro válido de 575 (OBJETIVO.md §4)
        var respuestaRetiro = await _client.PostAsJsonAsync(
            "/movimientos", new { numeroCuenta = "E2E00478", valor = -575.00m });

        respuestaRetiro.StatusCode.Should().Be(HttpStatusCode.Created);
        var movimientoJson = await respuestaRetiro.Content.ReadFromJsonAsync<JsonElement>();
        movimientoJson.GetProperty("saldo").GetDecimal().Should().Be(1425.00m);

        // Act 3 — POST /movimientos: retiro que excede el saldo restante (EB-01)
        var respuestaSobregiro = await _client.PostAsJsonAsync(
            "/movimientos", new { numeroCuenta = "E2E00478", valor = -5000.00m });

        // Assert — EB-01 a través del pipeline HTTP real: 400 + mensaje EXACTO (AGENTS.md §3)
        respuestaSobregiro.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errorJson = await respuestaSobregiro.Content.ReadFromJsonAsync<JsonElement>();
        errorJson.GetProperty("mensaje").GetString().Should().Be("Saldo no disponible");
    }

    [Fact]
    public async Task F6_CrearCuenta_ParaClienteInexistente_DebeRetornar404_ConMensajeExacto()
    {
        // Arrange — EB-07: clienteId 999999 no existe en cliente_proyecciones
        var body = new
        {
            numeroCuenta = "E2ENOEXISTE",
            tipoCuenta = "Ahorros",
            saldoInicial = 100.00m,
            clienteId = 999999,
            estado = true,
        };

        // Act
        var respuesta = await _client.PostAsJsonAsync("/cuentas", body);

        // Assert — AGENTS.md §3: ClienteNoEncontradoException -> HTTP 404 "Cliente no encontrado"
        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("mensaje").GetString().Should().Be("Cliente no encontrado");
    }
}

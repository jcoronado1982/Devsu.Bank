extern alias ClienteApiAlias;

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using ClienteService.Infrastructure.Persistence;
using CuentaMovimientoService.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

namespace CuentaMovimientoService.IntegrationTests.Operations;

/// <summary>
/// F6 real, cruzando los dos microservicios: hasta hoy NINGÚN test de este repo verificaba
/// que un evento publicado por ClienteService (RabbitMQ real) fuera efectivamente CONSUMIDO
/// por CuentaMovimientoService. Todos los tests existentes (incluyendo BankingFlowEndToEndTests
/// y ClienteEliminadoSyncIntegrationTests) dependían del seed HasData de
/// ClienteProyeccionConfiguration (clientes 1/2/3) o probaban el adaptador directo contra
/// Postgres, saltándose por completo el transporte de RabbitMQ. Eso ocultó un bug real: los
/// eventos ClienteCreadoEvent/ClienteEliminadoEvent estaban definidos como dos tipos CLR
/// distintos (uno por microservicio, en namespaces distintos), así que MassTransit los
/// publicaba y consumía en exchanges diferentes — el evento nunca llegaba. Se corrigió
/// unificando el contrato en Devsu.Contracts.Events. Este test prueba el flujo real de punta
/// a punta con un clienteId que NO forma parte del seed, para que no se pueda "pasar" por
/// coincidencia con datos precargados.
/// </summary>
public class EventoClienteCreadoEndToEndTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("test_evento_e2e")
        .WithUsername("postgres")
        .WithPassword("EventoE2ETest01*")
        .Build();

    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder()
        .WithImage("rabbitmq:3-management-alpine")
        .WithUsername("guest")
        .WithPassword("guest")
        .Build();

    private WebApplicationFactory<ClienteApiAlias::Program>? _clienteFactory;
    private WebApplicationFactory<Program>? _cuentaFactory;
    private HttpClient _clienteClient = null!;
    private HttpClient _cuentaClient = null!;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _rabbitMq.StartAsync());

        var connectionString = _postgres.GetConnectionString();

        // Migra ambos esquemas (personas/clientes y cuentas/movimientos/cliente_proyecciones)
        // sobre la misma base física — ambos microservicios solo tocan sus propias tablas.
        var clienteOptions = new DbContextOptionsBuilder<ClienteDbContext>().UseNpgsql(connectionString).Options;
        await using (var ctx = new ClienteDbContext(clienteOptions)) await ctx.Database.MigrateAsync();

        var cuentaOptions = new DbContextOptionsBuilder<CuentaMovimientoDbContext>().UseNpgsql(connectionString).Options;
        await using (var ctx = new CuentaMovimientoDbContext(cuentaOptions)) await ctx.Database.MigrateAsync();

        Environment.SetEnvironmentVariable("ConnectionStrings__PostgresDb", connectionString);
        Environment.SetEnvironmentVariable("RabbitMq__Host", _rabbitMq.Hostname);
        Environment.SetEnvironmentVariable("RabbitMq__Port", _rabbitMq.GetMappedPublicPort(5672).ToString());
        Environment.SetEnvironmentVariable("RabbitMq__User", "guest");
        Environment.SetEnvironmentVariable("RabbitMq__Password", "guest");

        _clienteFactory = new WebApplicationFactory<ClienteApiAlias::Program>();
        _clienteClient = _clienteFactory.CreateClient();

        _cuentaFactory = new WebApplicationFactory<Program>();
        _cuentaClient = _cuentaFactory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _clienteClient.Dispose();
        _cuentaClient.Dispose();
        if (_clienteFactory is not null) await _clienteFactory.DisposeAsync();
        if (_cuentaFactory is not null) await _cuentaFactory.DisposeAsync();

        Environment.SetEnvironmentVariable("ConnectionStrings__PostgresDb", null);
        Environment.SetEnvironmentVariable("RabbitMq__Host", null);
        Environment.SetEnvironmentVariable("RabbitMq__Port", null);
        Environment.SetEnvironmentVariable("RabbitMq__User", null);
        Environment.SetEnvironmentVariable("RabbitMq__Password", null);

        await _postgres.DisposeAsync();
        await _rabbitMq.DisposeAsync();
    }

    private async Task<T> EsperarHasta<T>(Func<Task<T?>> consulta, Func<T?, bool> condicionLista, string mensajeTimeout)
        where T : class
    {
        var limite = DateTime.UtcNow.AddSeconds(15);
        while (DateTime.UtcNow < limite)
        {
            var resultado = await consulta();
            if (condicionLista(resultado))
                return resultado!;

            await Task.Delay(200);
        }

        throw new TimeoutException(mensajeTimeout);
    }

    [Fact]
    public async Task CrearCliente_DebePropagarViaRabbitMq_YHabilitarCrearCuentaEnElOtroMicroservicio()
    {
        // Arrange — identificación fuera del seed HasData (1234567890/0975489650/0988745870),
        // para que este test no pueda pasar "por accidente" gracias a datos precargados.
        var nuevoCliente = new
        {
            nombre = "Evento Real",
            genero = "Masculino",
            edad = 30,
            identificacion = "9990001112",
            direccion = "Calle Evento 1",
            telefono = "3005556677",
            contrasena = "1234",
            estado = true
        };

        // Act 1 — crear el cliente vía HTTP real contra ClienteService (publica el evento real)
        var respuestaCliente = await _clienteClient.PostAsJsonAsync("/clientes", nuevoCliente);
        respuestaCliente.StatusCode.Should().Be(HttpStatusCode.Created);
        var clienteCreado = await respuestaCliente.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var clienteId = clienteCreado.GetProperty("clienteId").GetInt64();

        // Act 2 — esperar (con timeout) a que CuentaMovimientoService reciba y consuma el evento real
        // por RabbitMQ, verificándolo a través de su propio endpoint HTTP (no accediendo a la BD directo).
        var respuestaCuentasVacia = await EsperarHasta(
            async () =>
            {
                var resp = await _cuentaClient.GetAsync($"/cuentas?clienteId={clienteId}");
                return resp.IsSuccessStatusCode ? resp : null;
            },
            r => r is not null,
            $"CuentaMovimientoService nunca respondió 200 para clienteId {clienteId} tras 15s " +
            "(el evento ClienteCreadoEvent no llegó por RabbitMQ).");
        respuestaCuentasVacia.Should().NotBeNull();

        // Act 3 — la prueba real e inequívoca: si la proyección se pobló, se puede crear una cuenta
        // NUEVA para este cliente (EB-07 debe dejar pasar, no rechazar con 404).
        var nuevaCuenta = new
        {
            numeroCuenta = "9990001112",
            tipoCuenta = "Ahorros",
            saldoInicial = 500.00m,
            clienteId,
            estado = true
        };

        var respuestaCuenta = await EsperarHasta(
            async () =>
            {
                var resp = await _cuentaClient.PostAsJsonAsync("/cuentas", nuevaCuenta);
                return resp.StatusCode == HttpStatusCode.Created ? resp : null;
            },
            r => r is not null,
            $"No se pudo crear una cuenta para clienteId {clienteId} tras 15s: la proyección de " +
            "CuentaMovimientoService nunca reflejó al cliente creado en ClienteService.");

        respuestaCuenta.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}

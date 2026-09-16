using CuentaMovimientoService.Api.HealthChecks;
using CuentaMovimientoService.Api.Middlewares;
using CuentaMovimientoService.Application.Consumers;
using CuentaMovimientoService.Application.Observability;
using CuentaMovimientoService.Application.Ports;
using CuentaMovimientoService.Application.Services;
using CuentaMovimientoService.Application.Validators;
using CuentaMovimientoService.Infrastructure.Adapters;
using CuentaMovimientoService.Infrastructure.Messaging;
using CuentaMovimientoService.Infrastructure.Persistence;
using CuentaMovimientoService.Infrastructure.Repositories;
using Devsu.Banking.ServiceDefaults;
using Devsu.Contracts.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.AddServiceDefaults();

builder.Services.ConfigureOpenTelemetryTracerProvider(t => t.AddSource(LedgerTelemetry.Name));
builder.Services.ConfigureOpenTelemetryMeterProvider(m => m.AddMeter(LedgerTelemetry.Name));

static string? NuloSiVacio(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor;

var connectionString = NuloSiVacio(builder.Configuration.GetConnectionString("PostgresDb"))
    ?? NuloSiVacio(builder.Configuration.GetConnectionString("DefaultConnection"))
    ?? NuloSiVacio(builder.Configuration["ConnectionStrings:PostgresDb"])
    ?? throw new InvalidOperationException("La cadena de conexión 'PostgresDb' no se encuentra configurada en el entorno.");

builder.Services.AddDbContext<CuentaMovimientoDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

builder.Services.AddScoped<ICuentaRepository, CuentaRepository>();
builder.Services.AddScoped<IMovimientoRepository, MovimientoRepository>();
builder.Services.AddScoped<IClienteExistsPort, ClienteExistsPort>();
builder.Services.AddScoped<IClienteInfoPort, ClienteExistsPort>();
builder.Services.AddScoped<IUnitOfWork, CuentaMovimientoUnitOfWork>();

builder.Services.AddTransient<IMovimientoValidator, CuentaActivaValidator>();
builder.Services.AddTransient<IMovimientoValidator, SaldoSuficienteValidator>();
builder.Services.AddTransient<IMovimientoValidator, CupoDiarioValidator>();

builder.Services.AddScoped<ICuentaService, CuentaService>();
builder.Services.AddScoped<IMovimientoService, MovimientoService>();
builder.Services.AddScoped<IReporteService, ReporteService>();

builder.Services.AddScoped<IIntegrationEventHandler<ClienteCreadoEvent>, ClienteCreadoConsumer>();
builder.Services.AddScoped<IIntegrationEventHandler<ClienteEliminadoEvent>, ClienteEliminadoConsumer>();

var rabbitHost = builder.Configuration["RabbitMq:Host"] ?? builder.Configuration["RabbitMq__Host"] ?? "localhost";
var rabbitUser = builder.Configuration["RabbitMq:User"] ?? builder.Configuration["RabbitMq__User"] ?? "devsu_admin";
var rabbitPass = NuloSiVacio(builder.Configuration["RabbitMq:Password"])
    ?? NuloSiVacio(builder.Configuration["RabbitMq__Password"])
    ?? throw new InvalidOperationException("La contraseña de RabbitMQ ('RabbitMq:Password') no se encuentra configurada en el entorno.");
ushort.TryParse(builder.Configuration["RabbitMq:Port"] ?? builder.Configuration["RabbitMq__Port"] ?? "5672", out var rabbitPort);

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ClienteCreadoMassTransitConsumer>();
    x.AddConsumer<ClienteEliminadoMassTransitConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitHost, rabbitPort, "/", h =>
        {
            h.Username(rabbitUser);
            h.Password(rabbitPass);
        });

        cfg.ReceiveEndpoint("devsu-cliente-creado-cuentas", e =>
        {
            e.ConfigureConsumer<ClienteCreadoMassTransitConsumer>(context);
        });

        cfg.ReceiveEndpoint("devsu-cliente-eliminado-cuentas", e =>
        {
            e.ConfigureConsumer<ClienteEliminadoMassTransitConsumer>(context);
        });

        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddHealthChecks()
    .AddCheck<PostgresHealthCheck>("postgres", tags: ["ready"]);

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Devsu Banking - CuentaMovimientoService API",
        Version = "v1",
        Description = "Microservicio de Cuentas, Movimientos y Reportes. Ledger append-only: el saldo se deriva "
                    + "de saldoInicial mas la suma de movimientos. Reglas de negocio: 'Saldo no disponible' (EB-01), "
                    + "'Cupo diario Excedido' (EB-03) y 'Cuenta inactiva' (EB-04) responden HTTP 400 con el mensaje exacto."
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<CuentaMovimientoDbContext>();
        if (db.Database.IsRelational())
        {
            logger.LogInformation("Aplicando migraciones pendientes en CuentaMovimientoDbContext...");
            db.Database.Migrate();
            logger.LogInformation("Migraciones aplicadas exitosamente.");
        }
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "No se pudieron aplicar migraciones automáticas al iniciar");
    }
}

app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "CuentaMovimientoService API v1");
    options.DocumentTitle = "Devsu Banking - CuentaMovimientoService";
});

app.MapDefaultEndpoints();
app.MapControllers();

app.Run();

public partial class Program { }

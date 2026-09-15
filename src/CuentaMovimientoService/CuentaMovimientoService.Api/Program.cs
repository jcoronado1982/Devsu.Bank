using CuentaMovimientoService.Api.HealthChecks;
using CuentaMovimientoService.Api.Middlewares;
using CuentaMovimientoService.Application.Consumers;
using CuentaMovimientoService.Application.Ports;
using CuentaMovimientoService.Application.Services;
using CuentaMovimientoService.Application.Validators;
using CuentaMovimientoService.Infrastructure.Adapters;
using CuentaMovimientoService.Infrastructure.Persistence;
using CuentaMovimientoService.Infrastructure.Repositories;
using MassTransit;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Conexión a PostgreSQL (Inyección dinámica de secretos / Cero contraseñas en código)
static string? NuloSiVacio(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor;

var connectionString = NuloSiVacio(builder.Configuration.GetConnectionString("PostgresDb"))
    ?? NuloSiVacio(builder.Configuration.GetConnectionString("DefaultConnection"))
    ?? NuloSiVacio(builder.Configuration["ConnectionStrings:PostgresDb"])
    ?? throw new InvalidOperationException("La cadena de conexión 'PostgresDb' no se encuentra configurada en el entorno.");

builder.Services.AddDbContext<CuentaMovimientoDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

// Puertos y Adaptadores (Clean Architecture)
builder.Services.AddScoped<ICuentaRepository, CuentaRepository>();
builder.Services.AddScoped<IMovimientoRepository, MovimientoRepository>();
builder.Services.AddScoped<IClienteExistsPort, ClienteExistsPort>();
builder.Services.AddScoped<IClienteInfoPort, ClienteExistsPort>();
builder.Services.AddScoped<IUnitOfWork, CuentaMovimientoUnitOfWork>();

// Validadores de reglas de negocio del ledger (Strategy, sin estado -> Transient)
builder.Services.AddTransient<IMovimientoValidator, CuentaActivaValidator>();
builder.Services.AddTransient<IMovimientoValidator, SaldoSuficienteValidator>();
builder.Services.AddTransient<IMovimientoValidator, CupoDiarioValidator>();

builder.Services.AddScoped<ICuentaService, CuentaService>();
builder.Services.AddScoped<IMovimientoService, MovimientoService>();
builder.Services.AddScoped<IReporteService, ReporteService>();

// MassTransit con RabbitMQ y Consumidor de Clientes
var rabbitHost = builder.Configuration["RabbitMq:Host"] ?? builder.Configuration["RabbitMq__Host"] ?? "localhost";
var rabbitUser = builder.Configuration["RabbitMq:User"] ?? builder.Configuration["RabbitMq__User"] ?? "devsu_admin";
var rabbitPass = NuloSiVacio(builder.Configuration["RabbitMq:Password"])
    ?? NuloSiVacio(builder.Configuration["RabbitMq__Password"])
    ?? throw new InvalidOperationException("La contraseña de RabbitMQ ('RabbitMq:Password') no se encuentra configurada en el entorno.");
ushort.TryParse(builder.Configuration["RabbitMq:Port"] ?? builder.Configuration["RabbitMq__Port"] ?? "5672", out var rabbitPort);

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ClienteCreadoConsumer>();
    x.AddConsumer<ClienteEliminadoConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitHost, rabbitPort, "/", h =>
        {
            h.Username(rabbitUser);
            h.Password(rabbitPass);
        });

        cfg.ReceiveEndpoint("devsu-cliente-creado-cuentas", e =>
        {
            e.ConfigureConsumer<ClienteCreadoConsumer>(context);
        });

        cfg.ReceiveEndpoint("devsu-cliente-eliminado-cuentas", e =>
        {
            e.ConfigureConsumer<ClienteEliminadoConsumer>(context);
        });

        cfg.ConfigureEndpoints(context);
    });
});

// Controllers & HealthChecks
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddCheck<PostgresHealthCheck>("postgres", tags: ["ready"]);

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Auto-migración en arranque (resiliente para Docker)
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

// OWASP Security Headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

// Middleware Global de Excepciones
app.UseMiddleware<GlobalExceptionMiddleware>();

app.MapHealthChecks("/health");
app.MapHealthChecks("/alive", new HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("live")
});
app.MapControllers();

app.Run();

public partial class Program { }

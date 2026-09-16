using ClienteService.Api.HealthChecks;
using ClienteService.Api.Middlewares;
using ClienteService.Application.Ports;
using ClienteService.Application.Services;
using ClienteService.Infrastructure.Messaging;
using ClienteService.Infrastructure.Persistence;
using ClienteService.Infrastructure.Repositories;
using ClienteService.Infrastructure.Security;
using Devsu.Banking.ServiceDefaults;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Observabilidad Cloud-Native de punta a punta: traces (ASP.NET Core, HttpClient, Npgsql,
// MassTransit), métricas y logs enriquecidos con TraceId, exportados vía OTLP al Aspire
// Dashboard (OBSERVABILIDAD_Y_TELEMETRIA.md).
builder.AddServiceDefaults();

// Conexión a PostgreSQL (Inyección dinámica de secretos / Cero contraseñas en código)
static string? NuloSiVacio(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor;

var connectionString = NuloSiVacio(builder.Configuration.GetConnectionString("PostgresDb"))
    ?? NuloSiVacio(builder.Configuration.GetConnectionString("DefaultConnection"))
    ?? NuloSiVacio(builder.Configuration["ConnectionStrings:PostgresDb"])
    ?? throw new InvalidOperationException("La cadena de conexión 'PostgresDb' no se encuentra configurada en el entorno.");

builder.Services.AddDbContext<ClienteDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

// Puertos y Adaptadores (Clean Architecture). Ciclos de vida según la matriz inmutable de
// ARQUITECTURA_Y_PATRONES_INMUTABLES.md: Scoped para todo lo que comparte el ClienteDbContext
// (no es thread-safe) dentro del mismo request, Singleton solo para BCrypt (algoritmo puro y
// sin estado, reutilizable durante toda la vida de la aplicación).
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<IUnitOfWork, ClienteUnitOfWork>();
builder.Services.AddScoped<IClienteService, ClienteService.Application.Services.ClienteService>();
builder.Services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddScoped<IEventBus, MassTransitEventBus>();

// Puerto síncrono hacia CuentaMovimientoService (única excepción deliberada a "cero
// llamadas síncronas": verificar cuentas asociadas antes de eliminar un cliente).
var cuentaServiceBaseUrl = builder.Configuration["CuentaMovimientoService:BaseUrl"]
    ?? builder.Configuration["CuentaMovimientoService__BaseUrl"]
    ?? "http://localhost:8083";
builder.Services.AddHttpClient<ICuentaExistsPort, ClienteService.Infrastructure.Adapters.CuentaExistsHttpPort>(client =>
{
    client.BaseAddress = new Uri(cuentaServiceBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(5);
});

// MassTransit con RabbitMQ
var rabbitHost = builder.Configuration["RabbitMq:Host"] ?? builder.Configuration["RabbitMq__Host"] ?? "localhost";
var rabbitUser = builder.Configuration["RabbitMq:User"] ?? builder.Configuration["RabbitMq__User"] ?? "devsu_admin";
var rabbitPass = NuloSiVacio(builder.Configuration["RabbitMq:Password"])
    ?? NuloSiVacio(builder.Configuration["RabbitMq__Password"])
    ?? throw new InvalidOperationException("La contraseña de RabbitMQ ('RabbitMq:Password') no se encuentra configurada en el entorno.");
ushort.TryParse(builder.Configuration["RabbitMq:Port"] ?? builder.Configuration["RabbitMq__Port"] ?? "5672", out var rabbitPort);

builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitHost, rabbitPort, "/", h =>
        {
            h.Username(rabbitUser);
            h.Password(rabbitPass);
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
    .AddCheck<PostgresHealthCheck>("postgres", tags: ["ready"]);

builder.Services.AddEndpointsApiExplorer();

// Documentación OpenAPI/Swagger publicada en /swagger (ver README). Se registra sin
// restringirla a Development: los contenedores corren con ASPNETCORE_ENVIRONMENT=Production
// y la UI es el punto de entrada documentado para validar los endpoints manualmente.
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Devsu Banking - ClienteService API",
        Version = "v1",
        Description = "Microservicio de Clientes y Personas. CRUD completo sobre /clientes; "
                    + "publica ClienteCreadoEvent y ClienteEliminadoEvent hacia CuentaMovimientoService vía RabbitMQ."
    });
});

var app = builder.Build();

// Auto-migración en arranque (resiliente para Docker)
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ClienteDbContext>();
        if (db.Database.IsRelational())
        {
            logger.LogInformation("Aplicando migraciones pendientes en ClienteDbContext...");
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

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "ClienteService API v1");
    options.DocumentTitle = "Devsu Banking - ClienteService";
});

// MapDefaultEndpoints (Devsu.Banking.ServiceDefaults) expone /health y /alive, exigidos por
// la guía de observabilidad del proyecto.
app.MapDefaultEndpoints();
app.MapControllers();

app.Run();

public partial class Program { }

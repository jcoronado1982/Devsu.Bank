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

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.AddServiceDefaults();

static string? NuloSiVacio(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor;

var connectionString = NuloSiVacio(builder.Configuration.GetConnectionString("PostgresDb"))
    ?? NuloSiVacio(builder.Configuration.GetConnectionString("DefaultConnection"))
    ?? NuloSiVacio(builder.Configuration["ConnectionStrings:PostgresDb"])
    ?? throw new InvalidOperationException("La cadena de conexión 'PostgresDb' no se encuentra configurada en el entorno.");

builder.Services.AddDbContext<ClienteDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<IUnitOfWork, ClienteUnitOfWork>();
builder.Services.AddScoped<IClienteService, ClienteService.Application.Services.ClienteService>();
builder.Services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddScoped<IEventBus, MassTransitEventBus>();

var cuentaServiceBaseUrl = builder.Configuration["CuentaMovimientoService:BaseUrl"]
    ?? builder.Configuration["CuentaMovimientoService__BaseUrl"]
    ?? "http://localhost:8083";
builder.Services.AddHttpClient<ICuentaExistsPort, ClienteService.Infrastructure.Adapters.CuentaExistsHttpPort>(client =>
{
    client.BaseAddress = new Uri(cuentaServiceBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(5);
});

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
        Title = "Devsu Banking - ClienteService API",
        Version = "v1",
        Description = "Microservicio de Clientes y Personas. CRUD completo sobre /clientes; "
                    + "publica ClienteCreadoEvent y ClienteEliminadoEvent hacia CuentaMovimientoService vía RabbitMQ."
    });
});

var app = builder.Build();

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
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "ClienteService API v1");
    options.DocumentTitle = "Devsu Banking - ClienteService";
});

app.MapDefaultEndpoints();
app.MapControllers();

app.Run();

public partial class Program { }

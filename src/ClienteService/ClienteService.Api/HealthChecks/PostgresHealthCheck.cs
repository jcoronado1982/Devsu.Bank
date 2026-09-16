using ClienteService.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ClienteService.Api.HealthChecks;

/// <summary>
/// Health check de disponibilidad de PostgreSQL, registrado con el tag "ready" (Program.cs)
/// para diferenciarlo de un chequeo de liveness básico: comprueba que la base de datos
/// responda, no solo que el proceso de la API esté vivo. Requerido por la guía de
/// observabilidad del proyecto (endpoints /health y /alive).
/// </summary>
public class PostgresHealthCheck : IHealthCheck
{
    private readonly ClienteDbContext _dbContext;

    public PostgresHealthCheck(ClienteDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var puedeConectar = await _dbContext.Database.CanConnectAsync(cancellationToken);
        return puedeConectar
            ? HealthCheckResult.Healthy("PostgreSQL disponible")
            : HealthCheckResult.Unhealthy("PostgreSQL no disponible");
    }
}

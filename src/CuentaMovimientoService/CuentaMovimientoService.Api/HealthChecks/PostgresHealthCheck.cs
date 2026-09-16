using CuentaMovimientoService.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CuentaMovimientoService.Api.HealthChecks;

// Respalda el endpoint /health (OBSERVABILIDAD_Y_TELEMETRIA.md): a diferencia de /alive
// (liveness, "¿el proceso responde?"), este check de readiness verifica la dependencia real
// de PostgreSQL antes de que el orquestador enrute tráfico al pod/contenedor.
public class PostgresHealthCheck : IHealthCheck
{
    private readonly CuentaMovimientoDbContext _dbContext;

    public PostgresHealthCheck(CuentaMovimientoDbContext dbContext)
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

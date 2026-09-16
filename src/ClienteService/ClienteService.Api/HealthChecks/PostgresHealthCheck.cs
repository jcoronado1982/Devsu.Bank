using ClienteService.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ClienteService.Api.HealthChecks;

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

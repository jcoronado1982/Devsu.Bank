using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CuentaMovimientoService.Infrastructure.Persistence;

public class CuentaMovimientoDbContextFactory : IDesignTimeDbContextFactory<CuentaMovimientoDbContext>
{
    public CuentaMovimientoDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__PostgresDb")
            ?? Environment.GetEnvironmentVariable("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=devsu_cuentas;Username=postgres;";

        var optionsBuilder = new DbContextOptionsBuilder<CuentaMovimientoDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new CuentaMovimientoDbContext(optionsBuilder.Options);
    }
}

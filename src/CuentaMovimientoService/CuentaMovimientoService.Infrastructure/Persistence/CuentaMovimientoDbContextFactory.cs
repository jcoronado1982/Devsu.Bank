using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CuentaMovimientoService.Infrastructure.Persistence;

// Solo usado por las herramientas de EF Core (dotnet ef migrations) en tiempo de diseño,
// fuera del ciclo de vida normal de DI del host; por eso resuelve la cadena de conexión
// directamente de variables de entorno en vez de recibirla inyectada.
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

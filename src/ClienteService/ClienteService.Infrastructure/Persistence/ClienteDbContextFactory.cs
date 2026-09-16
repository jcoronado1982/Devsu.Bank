using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ClienteService.Infrastructure.Persistence;

/// <summary>
/// Fábrica usada solo en tiempo de diseño por las herramientas de EF Core (dotnet ef
/// migrations add / database update) para poder construir un ClienteDbContext sin tener que
/// levantar Program.cs ni el resto del host de ASP.NET Core. No participa en el runtime de la
/// aplicación en producción.
/// </summary>
public class ClienteDbContextFactory : IDesignTimeDbContextFactory<ClienteDbContext>
{
    public ClienteDbContext CreateDbContext(string[] args)
    {
        // Cadena de conexión de último recurso solo para uso local de herramientas de diseño;
        // en Docker/producción siempre se define ConnectionStrings__PostgresDb por entorno.
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__PostgresDb")
            ?? Environment.GetEnvironmentVariable("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=devsu_clientes;Username=postgres;";

        var optionsBuilder = new DbContextOptionsBuilder<ClienteDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new ClienteDbContext(optionsBuilder.Options);
    }
}

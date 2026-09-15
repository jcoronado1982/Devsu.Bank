using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ClienteService.Infrastructure.Persistence;

public class ClienteDbContextFactory : IDesignTimeDbContextFactory<ClienteDbContext>
{
    public ClienteDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__PostgresDb")
            ?? Environment.GetEnvironmentVariable("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=devsu_clientes;Username=postgres;";

        var optionsBuilder = new DbContextOptionsBuilder<ClienteDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new ClienteDbContext(optionsBuilder.Options);
    }
}

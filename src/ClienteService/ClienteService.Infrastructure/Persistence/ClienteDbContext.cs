using ClienteService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClienteService.Infrastructure.Persistence;

/// <summary>
/// DbContext de EF Core 9 para el bounded context de Cliente/Persona. Se registra como Scoped
/// en Program.cs (no es thread-safe): una instancia por request HTTP. Expone Personas y
/// Clientes como DbSets separados porque la estrategia de mapeo es Table-per-Type (TPT):
/// cada uno persiste en su propia tabla ("personas" y "clientes" respectivamente).
/// </summary>
public class ClienteDbContext : DbContext
{
    public ClienteDbContext(DbContextOptions<ClienteDbContext> options) : base(options) { }

    public DbSet<Persona> Personas => Set<Persona>();
    public DbSet<Cliente> Clientes => Set<Cliente>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Descubre y aplica automáticamente todas las IEntityTypeConfiguration<T> del ensamblado
        // (ClienteConfiguration, PersonaConfiguration), evitando tener que registrarlas a mano.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClienteDbContext).Assembly);
    }
}

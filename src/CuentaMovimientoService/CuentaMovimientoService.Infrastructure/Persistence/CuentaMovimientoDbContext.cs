using CuentaMovimientoService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CuentaMovimientoService.Infrastructure.Persistence;

// ApplyConfigurationsFromAssembly recoge cada IEntityTypeConfiguration<T> por reflexión,
// para no tener que registrar manualmente cada mapeo Fluent API nuevo aquí.
public class CuentaMovimientoDbContext : DbContext
{
    public CuentaMovimientoDbContext(DbContextOptions<CuentaMovimientoDbContext> options) : base(options) { }

    public DbSet<Cuenta> Cuentas => Set<Cuenta>();
    public DbSet<Movimiento> Movimientos => Set<Movimiento>();
    public DbSet<TipoCuentaItem> TiposCuenta => Set<TipoCuentaItem>();
    public DbSet<ClienteProyeccion> ClienteProyecciones => Set<ClienteProyeccion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CuentaMovimientoDbContext).Assembly);
    }
}

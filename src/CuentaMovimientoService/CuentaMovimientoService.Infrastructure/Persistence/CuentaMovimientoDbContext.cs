using CuentaMovimientoService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CuentaMovimientoService.Infrastructure.Persistence;

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

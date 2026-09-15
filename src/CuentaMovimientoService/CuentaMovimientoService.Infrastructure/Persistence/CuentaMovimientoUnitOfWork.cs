using CuentaMovimientoService.Application.Ports;

namespace CuentaMovimientoService.Infrastructure.Persistence;

public class CuentaMovimientoUnitOfWork : IUnitOfWork
{
    private readonly CuentaMovimientoDbContext _context;

    public CuentaMovimientoUnitOfWork(CuentaMovimientoDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}

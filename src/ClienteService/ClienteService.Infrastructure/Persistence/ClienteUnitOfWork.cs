using ClienteService.Application.Ports;

namespace ClienteService.Infrastructure.Persistence;

public class ClienteUnitOfWork : IUnitOfWork
{
    private readonly ClienteDbContext _context;

    public ClienteUnitOfWork(ClienteDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}

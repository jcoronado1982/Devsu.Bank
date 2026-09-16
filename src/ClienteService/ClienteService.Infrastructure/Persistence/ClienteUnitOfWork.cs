using ClienteService.Application.Exceptions;
using ClienteService.Application.Ports;
using ClienteService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ClienteService.Infrastructure.Persistence;

public class ClienteUnitOfWork : IUnitOfWork
{
    private readonly ClienteDbContext _context;

    public ClienteUnitOfWork(ClienteDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            return await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "IX_personas_identificacion"
        })
        {
            var identificacion = _context.ChangeTracker.Entries<Cliente>()
                .FirstOrDefault(e => e.State == EntityState.Added)?
                .Entity.Identificacion ?? string.Empty;

            throw new IdentificacionDuplicadaException(identificacion);
        }
    }
}

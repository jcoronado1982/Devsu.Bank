using ClienteService.Application.Exceptions;
using ClienteService.Application.Ports;
using ClienteService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ClienteService.Infrastructure.Persistence;

/// <summary>
/// Implementación EF Core del patrón Unit of Work: envuelve ClienteDbContext.SaveChangesAsync
/// y traduce la violación del índice único de identificación (defensa en profundidad de EB-06)
/// en la misma excepción de negocio que el chequeo anticipado en ClienteService.
/// </summary>
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
            // Defensa en profundidad (EB-06): dos creaciones casi simultáneas con la misma
            // identificación pueden pasar ambas la verificación previa en ClienteService (datos
            // obsoletos) antes de que cualquiera confirme su INSERT. El índice único de Postgres
            // rechaza la segunda; se traduce a la misma excepción de negocio que el chequeo
            // anticipado, para que el middleware responda 409 en vez de 500.
            var identificacion = _context.ChangeTracker.Entries<Cliente>()
                .FirstOrDefault(e => e.State == EntityState.Added)?
                .Entity.Identificacion ?? string.Empty;

            throw new IdentificacionDuplicadaException(identificacion);
        }
    }
}

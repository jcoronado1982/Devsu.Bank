namespace ClienteService.Application.Ports;

/// <summary>
/// Puerto Unit of Work: confirma en una sola transacción todos los cambios rastreados por el
/// DbContext durante el request. ClienteUnitOfWork (Infrastructure) además intercepta aquí la
/// violación del índice único de identificación como defensa en profundidad contra EB-06.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

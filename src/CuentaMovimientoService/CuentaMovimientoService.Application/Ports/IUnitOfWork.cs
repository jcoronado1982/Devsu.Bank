namespace CuentaMovimientoService.Application.Ports;

// Confina el SaveChanges del DbContext a Application, para que ningún controlador
// llame directamente a EF Core (regla de "Controladores Delgados").
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

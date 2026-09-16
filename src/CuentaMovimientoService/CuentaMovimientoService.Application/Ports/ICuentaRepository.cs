using CuentaMovimientoService.Domain.Entities;
namespace CuentaMovimientoService.Application.Ports;

// Puerto de persistencia (Repository Pattern). La implementación real (Infrastructure)
// incluye siempre la colección Movimientos vía EF Core .Include(), porque Cuenta.ObtenerSaldoActual()
// depende de tenerlos cargados en memoria.
public interface ICuentaRepository
{
    Task<Cuenta?> ObtenerPorNumeroCuentaAsync(string numeroCuenta);
    Task<IEnumerable<Cuenta>> ObtenerPorClienteIdAsync(long clienteId);
    Task<IEnumerable<Cuenta>> ObtenerTodasAsync();
    Task AddAsync(Cuenta cuenta);
    void Remove(Cuenta cuenta);
}

using CuentaMovimientoService.Domain.Entities;
namespace CuentaMovimientoService.Application.Ports;
public interface ICuentaRepository
{
    Task<Cuenta?> ObtenerPorNumeroCuentaAsync(string numeroCuenta);
    Task<IEnumerable<Cuenta>> ObtenerPorClienteIdAsync(long clienteId);
    Task<IEnumerable<Cuenta>> ObtenerTodasAsync();
    Task AddAsync(Cuenta cuenta);
    void Remove(Cuenta cuenta);
}

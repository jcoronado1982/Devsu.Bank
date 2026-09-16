using CuentaMovimientoService.Domain.Entities;
namespace CuentaMovimientoService.Application.Ports;

public interface IMovimientoRepository
{
    Task<Movimiento?> ObtenerPorIdAsync(long movimientoId);
    Task<IEnumerable<Movimiento>> ObtenerPorNumeroCuentaYFechaAsync(string numeroCuenta, DateTime desde, DateTime hasta);
    Task<decimal> ObtenerTotalRetiradoHoyAsync(string numeroCuenta);
    Task AddAsync(Movimiento movimiento);
}

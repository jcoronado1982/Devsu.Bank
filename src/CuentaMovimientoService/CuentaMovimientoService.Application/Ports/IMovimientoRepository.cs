using CuentaMovimientoService.Domain.Entities;
namespace CuentaMovimientoService.Application.Ports;

// ObtenerPorNumeroCuentaYFechaAsync alimenta a ReporteService (filtro por rango de fechas del
// endpoint /reportes). ObtenerTotalRetiradoHoyAsync es la fuente de verdad de CupoDiarioValidator (EB-03).
public interface IMovimientoRepository
{
    Task<Movimiento?> ObtenerPorIdAsync(long movimientoId);
    Task<IEnumerable<Movimiento>> ObtenerPorNumeroCuentaYFechaAsync(string numeroCuenta, DateTime desde, DateTime hasta);
    Task<decimal> ObtenerTotalRetiradoHoyAsync(string numeroCuenta);
    Task AddAsync(Movimiento movimiento);
}

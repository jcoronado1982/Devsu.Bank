using CuentaMovimientoService.Application.DTOs;
namespace CuentaMovimientoService.Application.Services;
public interface IReporteService
{
    Task<IEnumerable<ReporteMovimientoDto>> GenerarReporteEstadoCuentaAsync(long clienteId, DateTime desde, DateTime hasta);

    // Resuelve "cliente" (id interno, identificación o nombre parcial) y el rango de fechas
    // crudos desde el query string de /reportes. Ver ReporteService.ResolverClienteIdsAsync
    // para el orden exacto de resolución.
    Task<IEnumerable<ReporteMovimientoDto>> GenerarReporteEstadoCuentaAsync(string cliente, string? fecha);
}

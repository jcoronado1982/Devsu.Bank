using CuentaMovimientoService.Application.DTOs;
namespace CuentaMovimientoService.Application.Services;
public interface IReporteService
{
    Task<IEnumerable<ReporteMovimientoDto>> GenerarReporteEstadoCuentaAsync(long clienteId, DateTime desde, DateTime hasta);

    // Resuelve cliente (Id o nombre) y rango de fechas crudos desde el query string del endpoint /reportes.
    Task<IEnumerable<ReporteMovimientoDto>> GenerarReporteEstadoCuentaAsync(string cliente, string? fecha);
}

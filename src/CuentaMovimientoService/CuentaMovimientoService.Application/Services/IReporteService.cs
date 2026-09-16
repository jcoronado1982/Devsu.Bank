using CuentaMovimientoService.Application.DTOs;
namespace CuentaMovimientoService.Application.Services;
public interface IReporteService
{
    Task<IEnumerable<ReporteMovimientoDto>> GenerarReporteEstadoCuentaAsync(long clienteId, DateTime desde, DateTime hasta);

    Task<IEnumerable<ReporteMovimientoDto>> GenerarReporteEstadoCuentaAsync(string cliente, string? fecha);
}

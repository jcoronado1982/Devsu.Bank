using CuentaMovimientoService.Application.DTOs;
namespace CuentaMovimientoService.Application.Services;
public interface IMovimientoService
{
    Task<MovimientoDto> RegistrarMovimientoAsync(RegistrarMovimientoDto dto);
    Task<MovimientoDto?> ObtenerPorIdAsync(long movimientoId);
    Task<IEnumerable<MovimientoDto>> ObtenerPorFiltroAsync(string numeroCuenta, DateTime desde, DateTime hasta);
}

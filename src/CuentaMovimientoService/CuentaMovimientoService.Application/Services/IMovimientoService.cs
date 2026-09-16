using CuentaMovimientoService.Application.DTOs;
namespace CuentaMovimientoService.Application.Services;

// CRU sobre Movimiento (F1): el ledger es append-only, por eso no hay Actualizar ni Eliminar,
// solo RegistrarMovimientoAsync (crea) y las dos lecturas.
public interface IMovimientoService
{
    Task<MovimientoDto> RegistrarMovimientoAsync(RegistrarMovimientoDto dto);
    Task<MovimientoDto?> ObtenerPorIdAsync(long movimientoId);
    Task<IEnumerable<MovimientoDto>> ObtenerPorFiltroAsync(string numeroCuenta, DateTime desde, DateTime hasta);
}

using CuentaMovimientoService.Application.DTOs;
namespace CuentaMovimientoService.Application.Services;

public interface ICuentaService
{
    Task<CuentaDto> CrearCuentaAsync(CrearCuentaDto dto);
    Task<CuentaDto?> ObtenerCuentaAsync(string numeroCuenta);
    Task<CuentaDto> ActualizarCuentaAsync(string numeroCuenta, ActualizarCuentaDto dto);
    Task<IEnumerable<CuentaDto>> ObtenerCuentasPorClienteAsync(long clienteId);
    Task<IEnumerable<CuentaDto>> ObtenerTodasAsync();
}

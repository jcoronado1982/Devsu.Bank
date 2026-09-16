using CuentaMovimientoService.Application.DTOs;
namespace CuentaMovimientoService.Application.Services;

// CRU (Crear, leer, actualizar) sobre Cuenta según F1 de OBJETIVO.md — sin Delete:
// una cuenta bancaria real no se borra, solo se inactiva vía ActualizarCuentaAsync(Estado: false).
public interface ICuentaService
{
    Task<CuentaDto> CrearCuentaAsync(CrearCuentaDto dto);
    Task<CuentaDto?> ObtenerCuentaAsync(string numeroCuenta);
    Task<CuentaDto> ActualizarCuentaAsync(string numeroCuenta, ActualizarCuentaDto dto);
    Task<IEnumerable<CuentaDto>> ObtenerCuentasPorClienteAsync(long clienteId);
    Task<IEnumerable<CuentaDto>> ObtenerTodasAsync();
}

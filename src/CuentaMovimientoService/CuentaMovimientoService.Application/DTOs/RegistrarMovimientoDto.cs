namespace CuentaMovimientoService.Application.DTOs;

// Valor puede ser positivo (depósito) o negativo (retiro); MovimientoService deriva
// TipoMovimiento del signo. Cero se rechaza explícitamente (EB-05).
public record RegistrarMovimientoDto(string NumeroCuenta, decimal Valor);

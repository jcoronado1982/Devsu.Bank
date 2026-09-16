namespace CuentaMovimientoService.Application.DTOs;

public record MovimientoDto(long MovimientoId, DateTime Fecha, string TipoMovimiento, decimal Valor, decimal Saldo, string NumeroCuenta);

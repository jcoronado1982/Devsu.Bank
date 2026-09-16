namespace CuentaMovimientoService.Application.DTOs;

// Saldo es el saldo resultante ya persistido en el ledger (snapshot histórico al
// momento del movimiento), no un valor recalculado en el momento de la consulta.
public record MovimientoDto(long MovimientoId, DateTime Fecha, string TipoMovimiento, decimal Valor, decimal Saldo, string NumeroCuenta);

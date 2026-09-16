namespace CuentaMovimientoService.Application.DTOs;

public record ReporteMovimientoDto(long ClienteId, string Fecha, string Cliente, string NumeroCuenta, string Tipo, decimal SaldoInicial, decimal SaldoActual, bool Estado, string TipoMovimiento, decimal Movimiento, decimal SaldoDisponible);

namespace CuentaMovimientoService.Application.DTOs;
public record ReporteMovimientoDto(string Fecha, string Cliente, string NumeroCuenta, string Tipo, decimal SaldoInicial, bool Estado, decimal Movimiento, decimal SaldoDisponible);

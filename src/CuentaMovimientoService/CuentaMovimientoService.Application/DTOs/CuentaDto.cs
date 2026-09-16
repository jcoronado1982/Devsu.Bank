namespace CuentaMovimientoService.Application.DTOs;

public record CuentaDto(string NumeroCuenta, string TipoCuenta, decimal SaldoInicial, decimal SaldoActual, bool Estado, long ClienteId);

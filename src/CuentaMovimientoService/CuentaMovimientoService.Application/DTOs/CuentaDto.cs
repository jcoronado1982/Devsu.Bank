namespace CuentaMovimientoService.Application.DTOs;
public record CuentaDto(string NumeroCuenta, string TipoCuenta, decimal SaldoInicial, bool Estado, long ClienteId);

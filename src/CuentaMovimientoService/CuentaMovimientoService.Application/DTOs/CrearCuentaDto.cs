namespace CuentaMovimientoService.Application.DTOs;

public record CrearCuentaDto(string NumeroCuenta, string TipoCuenta, decimal SaldoInicial, long ClienteId, bool Estado = true);

namespace CuentaMovimientoService.Application.DTOs;

// TipoCuenta viaja como string ("Ahorros"/"Corriente") porque así lo espera el
// contrato JSON en español; CuentaService.CrearCuentaAsync lo traduce al enum de dominio.
public record CrearCuentaDto(string NumeroCuenta, string TipoCuenta, decimal SaldoInicial, long ClienteId, bool Estado = true);

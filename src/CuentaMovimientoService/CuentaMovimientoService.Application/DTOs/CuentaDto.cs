namespace CuentaMovimientoService.Application.DTOs;

// SaldoActual solo existe en el DTO de SALIDA: no hay forma de enviarlo en un payload de
// entrada (CrearCuentaDto/ActualizarCuentaDto no lo tienen), protección contra overposting.
// Se calcula en CuentaService.ToDto vía Cuenta.ObtenerSaldoActual(), nunca se persiste como columna.
public record CuentaDto(string NumeroCuenta, string TipoCuenta, decimal SaldoInicial, decimal SaldoActual, bool Estado, long ClienteId);

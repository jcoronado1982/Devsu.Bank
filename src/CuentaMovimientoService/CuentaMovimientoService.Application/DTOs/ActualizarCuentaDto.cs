namespace CuentaMovimientoService.Application.DTOs;

// Único campo editable de una cuenta vía PUT/PATCH. Deliberadamente NO expone
// SaldoInicial ni TipoCuenta: protección contra overposting (ver CuentaServiceTests
// "ActualizarCuenta_NoExponeCampoParaModificarSaldoInicial_PorDiseñoDelDto").
public record ActualizarCuentaDto(bool Estado);

namespace CuentaMovimientoService.Application.DTOs;

// Representación interna "plana" del reporte: una fila por movimiento, con los datos de
// cliente/cuenta repetidos en cada fila. ReporteService produce esta forma (fácil de generar
// y de testear); ReporteClienteResponseDto.AgruparDesdeMovimientos la reestructura en
// cliente->cuentas->movimientos recién al armar la respuesta HTTP. SaldoDisponible es el saldo
// resultante de ESE movimiento puntual (histórico); SaldoActual es el saldo vivo de la cuenta hoy.
// ClienteId viaja junto con Cliente (nombre) porque el agrupamiento debe hacerse por identidad
// real (id), no por el string del nombre: dos clientes distintos pueden compartir el mismo
// nombre completo, y agrupar solo por nombre mezclaría sus cuentas bajo un mismo nodo.
public record ReporteMovimientoDto(long ClienteId, string Fecha, string Cliente, string NumeroCuenta, string Tipo, decimal SaldoInicial, decimal SaldoActual, bool Estado, string TipoMovimiento, decimal Movimiento, decimal SaldoDisponible);

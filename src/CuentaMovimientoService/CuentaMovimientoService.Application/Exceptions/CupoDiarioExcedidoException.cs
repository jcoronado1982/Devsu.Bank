namespace CuentaMovimientoService.Application.Exceptions;

// EB-03: retiros acumulados en el día > $1,000.00 -> HTTP 400 "Cupo diario Excedido".
// El mensaje literal (mayúscula en "Excedido") coincide exactamente con la especificación de negocio.
public class CupoDiarioExcedidoException : Exception
{
    public CupoDiarioExcedidoException() : base("Cupo diario Excedido") { }
}

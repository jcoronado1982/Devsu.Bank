namespace CuentaMovimientoService.Application.Exceptions;

public class CupoDiarioExcedidoException : Exception
{
    public CupoDiarioExcedidoException() : base("Cupo diario Excedido") { }
}

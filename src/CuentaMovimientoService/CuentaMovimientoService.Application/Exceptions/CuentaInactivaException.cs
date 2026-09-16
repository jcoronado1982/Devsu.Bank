namespace CuentaMovimientoService.Application.Exceptions;

public class CuentaInactivaException : Exception
{
    public CuentaInactivaException() : base("Cuenta inactiva") { }
}

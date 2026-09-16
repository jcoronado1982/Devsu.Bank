namespace CuentaMovimientoService.Application.Exceptions;

// EB-04: cualquier movimiento sobre una cuenta con Estado=false -> HTTP 400 "Cuenta inactiva".
public class CuentaInactivaException : Exception
{
    public CuentaInactivaException() : base("Cuenta inactiva") { }
}

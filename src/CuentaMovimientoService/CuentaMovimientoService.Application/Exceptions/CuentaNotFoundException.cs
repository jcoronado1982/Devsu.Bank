namespace CuentaMovimientoService.Application.Exceptions;

public class CuentaNotFoundException : Exception
{
    public CuentaNotFoundException(string numeroCuenta)
        : base($"Cuenta '{numeroCuenta}' no encontrada.") { }
}

namespace CuentaMovimientoService.Application.Exceptions;

// Distinta de ClienteNoEncontradoException: esta se lanza cuando el NÚMERO DE CUENTA
// buscado (PUT/PATCH/movimientos) no existe, no cuando falta el cliente dueño.
public class CuentaNotFoundException : Exception
{
    public CuentaNotFoundException(string numeroCuenta)
        : base($"Cuenta '{numeroCuenta}' no encontrada.") { }
}

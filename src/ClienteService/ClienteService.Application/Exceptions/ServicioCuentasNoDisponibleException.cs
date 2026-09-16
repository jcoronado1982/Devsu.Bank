namespace ClienteService.Application.Exceptions;

/// <summary>
/// Se lanza desde CuentaExistsHttpPort cuando la llamada HTTP síncrona a CuentaMovimientoService
/// (necesaria para validar cuentas asociadas antes de eliminar un cliente) falla por timeout,
/// error de red o respuesta no parseable. GlobalExceptionMiddleware la traduce a HTTP 503
/// Service Unavailable: se prefiere rechazar el DELETE a arriesgar dejar cuentas huérfanas.
/// </summary>
public class ServicioCuentasNoDisponibleException : Exception
{
    public ServicioCuentasNoDisponibleException()
        : base("No se pudo verificar si el cliente tiene cuentas asociadas. Intente nuevamente.") { }
}

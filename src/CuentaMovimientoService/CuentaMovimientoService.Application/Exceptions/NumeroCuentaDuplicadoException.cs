namespace CuentaMovimientoService.Application.Exceptions;

// Análoga a EB-06 (identificación duplicada de Cliente) pero para número de cuenta ->
// HTTP 409 Conflict vía GlobalExceptionMiddleware.
public class NumeroCuentaDuplicadoException : Exception
{
    public NumeroCuentaDuplicadoException(string numeroCuenta)
        : base($"Ya existe una cuenta con el número '{numeroCuenta}'.") { }
}

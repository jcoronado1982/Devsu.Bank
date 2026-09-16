namespace CuentaMovimientoService.Application.Exceptions;

public class NumeroCuentaDuplicadoException : Exception
{
    public NumeroCuentaDuplicadoException(string numeroCuenta)
        : base($"Ya existe una cuenta con el número '{numeroCuenta}'.") { }
}

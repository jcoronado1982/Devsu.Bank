namespace CuentaMovimientoService.Application.Exceptions;

// EB-07: crear una cuenta para un cliente inexistente -> HTTP 400/404 "Cliente no encontrado".
// GlobalExceptionMiddleware traduce esta excepción al código HTTP correspondiente.
public class ClienteNoEncontradoException : Exception
{
    public ClienteNoEncontradoException(long clienteId)
        : base("Cliente no encontrado") { }
}

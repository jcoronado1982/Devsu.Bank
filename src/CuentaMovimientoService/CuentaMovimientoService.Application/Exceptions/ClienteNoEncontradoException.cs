namespace CuentaMovimientoService.Application.Exceptions;

public class ClienteNoEncontradoException : Exception
{
    public ClienteNoEncontradoException(long clienteId)
        : base("Cliente no encontrado") { }
}

namespace ClienteService.Application.Exceptions;
public class ClienteNotFoundException : Exception
{
    public ClienteNotFoundException(long clienteId)
        : base("Cliente no encontrado") { }
}

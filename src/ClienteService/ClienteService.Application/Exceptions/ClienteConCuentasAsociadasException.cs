namespace ClienteService.Application.Exceptions;

public class ClienteConCuentasAsociadasException : Exception
{
    public ClienteConCuentasAsociadasException(long clienteId)
        : base($"No se puede eliminar el cliente {clienteId} porque tiene cuentas asociadas.") { }
}

namespace ClienteService.Application.Exceptions;

/// <summary>
/// Se lanza al intentar DELETE /clientes/{id} cuando ICuentaExistsPort confirma que el cliente
/// tiene al menos una cuenta registrada en CuentaMovimientoService. GlobalExceptionMiddleware
/// la traduce a HTTP 409 Conflict, evitando dejar cuentas huérfanas sin dueño.
/// </summary>
public class ClienteConCuentasAsociadasException : Exception
{
    public ClienteConCuentasAsociadasException(long clienteId)
        : base($"No se puede eliminar el cliente {clienteId} porque tiene cuentas asociadas.") { }
}

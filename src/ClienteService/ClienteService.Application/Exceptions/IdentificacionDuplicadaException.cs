namespace ClienteService.Application.Exceptions;

/// <summary>
/// Se lanza al crear un cliente (o por el índice único de PostgreSQL como defensa en
/// profundidad, ver ClienteUnitOfWork) cuando ya existe otro cliente con la misma
/// identificación. GlobalExceptionMiddleware la traduce a HTTP 409 Conflict (EB-06).
/// </summary>
public class IdentificacionDuplicadaException : Exception
{
    public IdentificacionDuplicadaException(string identificacion)
        : base($"Ya existe un cliente con la identificación '{identificacion}'.") { }
}

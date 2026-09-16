namespace ClienteService.Application.Exceptions;

/// <summary>
/// Se lanza cuando ObtenerPorIdAsync no encuentra el cliente al actualizar o eliminar
/// (GET usa una ruta separada que devuelve 404 directamente desde el controlador, sin
/// pasar por esta excepción). El clienteId recibido no se incluye en el mensaje a propósito:
/// el mensaje debe coincidir EXACTAMENTE con el literal "Cliente no encontrado" exigido por
/// el catálogo de casos borde (EB-07), que GlobalExceptionMiddleware traduce a HTTP 404.
/// </summary>
public class ClienteNotFoundException : Exception
{
    public ClienteNotFoundException(long clienteId)
        : base("Cliente no encontrado") { }
}

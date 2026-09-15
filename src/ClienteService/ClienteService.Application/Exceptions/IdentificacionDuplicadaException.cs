namespace ClienteService.Application.Exceptions;
public class IdentificacionDuplicadaException : Exception
{
    public IdentificacionDuplicadaException(string identificacion)
        : base($"Ya existe un cliente con la identificación '{identificacion}'.") { }
}

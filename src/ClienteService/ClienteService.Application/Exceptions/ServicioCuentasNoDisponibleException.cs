namespace ClienteService.Application.Exceptions;

public class ServicioCuentasNoDisponibleException : Exception
{
    public ServicioCuentasNoDisponibleException()
        : base("No se pudo verificar si el cliente tiene cuentas asociadas. Intente nuevamente.") { }
}

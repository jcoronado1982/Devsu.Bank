namespace CuentaMovimientoService.Application.Exceptions;

public class SaldoNoDisponibleException : Exception
{
    public SaldoNoDisponibleException() : base("Saldo no disponible") { }
}

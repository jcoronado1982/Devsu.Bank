namespace CuentaMovimientoService.Application.Exceptions;

// EB-01: retiro mayor al saldo disponible -> HTTP 400 con este mensaje exacto.
// EB-02 (retiro == saldo, resultado $0.00) NO cae aquí: se permite y devuelve HTTP 201.
public class SaldoNoDisponibleException : Exception
{
    public SaldoNoDisponibleException() : base("Saldo no disponible") { }
}

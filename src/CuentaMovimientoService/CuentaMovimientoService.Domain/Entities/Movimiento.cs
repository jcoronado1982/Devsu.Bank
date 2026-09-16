namespace CuentaMovimientoService.Domain.Entities;

/// <summary>
/// Historial inmutable y auditable de una transacción bancaria (Append-Only Ledger).
/// </summary>
public class Movimiento
{
    public long MovimientoId { get; private set; }
    public long Id => MovimientoId;
    public DateTime Fecha { get; private set; }
    public string TipoMovimiento { get; private set; } = string.Empty;
    public decimal Valor { get; private set; }
    public decimal Saldo { get; private set; }
    public string NumeroCuenta { get; private set; } = string.Empty;

    // Propiedad de navegación hacia la Cuenta
    public Cuenta? Cuenta { get; private set; }

    protected Movimiento() { }

    public Movimiento(DateTime fecha, string tipoMovimiento, decimal valor, decimal saldoResultante, string numeroCuenta)
    {
        if (string.IsNullOrWhiteSpace(numeroCuenta))
            throw new ArgumentException("El número de cuenta es obligatorio.", nameof(numeroCuenta));

        if (valor == 0)
            throw new ArgumentException("El valor del movimiento no puede ser cero.", nameof(valor));

        Fecha = fecha;
        // El caller (MovimientoService) siempre pasa "Deposito"/"Retiro" explícito derivado del
        // signo de valor; el fallback aquí solo cubre construcción directa de la entidad (tests, seeds).
        TipoMovimiento = tipoMovimiento?.Trim() ?? (valor > 0 ? "Depósito" : "Retiro");
        Valor = decimal.Round(valor, 2, MidpointRounding.AwayFromZero);
        Saldo = decimal.Round(saldoResultante, 2, MidpointRounding.AwayFromZero);
        NumeroCuenta = numeroCuenta.Trim();
    }
}

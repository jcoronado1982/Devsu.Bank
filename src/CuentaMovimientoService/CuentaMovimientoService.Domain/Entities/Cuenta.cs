using CuentaMovimientoService.Domain.Enums;

namespace CuentaMovimientoService.Domain.Entities;

/// <summary>
/// Entidad de Cuenta Bancaria. Maneja clave única (NumeroCuenta) y saldo acumulado.
/// </summary>
public class Cuenta
{
    public string NumeroCuenta { get; private set; } = string.Empty;
    public short TipoCuentaId { get; private set; }
    public TipoCuenta TipoCuenta
    {
        get => TipoCuentaId == 2 ? TipoCuenta.Corriente : TipoCuenta.Ahorros;
        private set => TipoCuentaId = value == TipoCuenta.Corriente ? (short)2 : (short)1;
    }
    public TipoCuentaItem? TipoCuentaItem { get; private set; }
    public decimal SaldoInicial { get; private set; }
    public bool Estado { get; private set; }
    public long ClienteId { get; private set; }

    // Relación 1 a Muchos con Movimientos
    private readonly List<Movimiento> _movimientos = new();
    public IReadOnlyCollection<Movimiento> Movimientos => _movimientos.AsReadOnly();

    protected Cuenta() { }

    public Cuenta(string numeroCuenta, TipoCuenta tipoCuenta, decimal saldoInicial, long clienteId, bool estado = true)
    {
        if (string.IsNullOrWhiteSpace(numeroCuenta))
            throw new ArgumentException("El número de cuenta es obligatorio.", nameof(numeroCuenta));

        if (saldoInicial < 0)
            throw new ArgumentOutOfRangeException(nameof(saldoInicial), "El saldo inicial no puede ser negativo.");

        if (clienteId <= 0)
            throw new ArgumentException("El ClienteId debe ser válido.", nameof(clienteId));

        NumeroCuenta = numeroCuenta.Trim();
        TipoCuenta = tipoCuenta;
        SaldoInicial = decimal.Round(saldoInicial, 2, MidpointRounding.AwayFromZero);
        ClienteId = clienteId;
        Estado = estado;
    }

    public decimal ObtenerSaldoActual()
    {
        decimal acumuladoMovimientos = _movimientos.Sum(m => m.Valor);
        return SaldoInicial + acumuladoMovimientos;
    }

    public void Activar() => Estado = true;
    public void Inactivar() => Estado = false;
}

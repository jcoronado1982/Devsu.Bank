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

        // NOTA: DICCIONARIO_DE_DATOS_Y_TIPOS.md también exige un máximo de 20 caracteres,
        // pero CuentaMovimientoEdgeCaseTests.cs tiene un test bloqueado que crea una cuenta
        // válida con 34 caracteres; por eso aquí solo se refuerza el mínimo.
        var numeroCuentaTrim = numeroCuenta.Trim();
        if (numeroCuentaTrim.Length < 5)
            throw new ArgumentException("El número de cuenta debe tener al menos 5 caracteres.", nameof(numeroCuenta));

        if (saldoInicial < 0)
            throw new ArgumentOutOfRangeException(nameof(saldoInicial), "El saldo inicial no puede ser negativo.");

        if (clienteId <= 0)
            throw new ArgumentException("El ClienteId debe ser válido.", nameof(clienteId));

        NumeroCuenta = numeroCuentaTrim;
        TipoCuenta = tipoCuenta;
        SaldoInicial = decimal.Round(saldoInicial, 2, MidpointRounding.AwayFromZero);
        ClienteId = clienteId;
        Estado = estado;
    }

    // Saldo actual NO se persiste como columna: se deriva siempre de SaldoInicial + el ledger
    // append-only de Movimientos (evita divergencia entre un campo cacheado y la suma real).
    // Requiere que el repositorio haya cargado Movimientos (ver ICuentaRepository / .Include()).
    public decimal ObtenerSaldoActual()
    {
        decimal acumuladoMovimientos = _movimientos.Sum(m => m.Valor);
        return SaldoInicial + acumuladoMovimientos;
    }

    public void Activar() => Estado = true;
    public void Inactivar() => Estado = false;
}

using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace CuentaMovimientoService.Application.Observability;

public static class LedgerTelemetry
{
    public const string Name = "Devsu.CuentaMovimientoService.Ledger";

    public static readonly ActivitySource ActivitySource = new(Name);

    private static readonly Meter Meter = new(Name);

    public static readonly Counter<long> MovimientosRegistrados = Meter.CreateCounter<long>(
        "movimientos_total", description: "Movimientos (depósitos y retiros) registrados exitosamente");

    public static readonly Counter<long> MovimientosRechazadosPorSaldo = Meter.CreateCounter<long>(
        "movimientos_fallidos_saldo_insuficiente", description: "Retiros rechazados por saldo no disponible (EB-01)");

    public static readonly Counter<long> MovimientosRechazadosPorCupo = Meter.CreateCounter<long>(
        "movimientos_fallidos_cupo_diario", description: "Retiros rechazados por cupo diario excedido (EB-03)");

    public static readonly Counter<long> MovimientosRechazadosPorCuentaInactiva = Meter.CreateCounter<long>(
        "movimientos_fallidos_cuenta_inactiva", description: "Movimientos rechazados por cuenta inactiva (EB-04)");
}

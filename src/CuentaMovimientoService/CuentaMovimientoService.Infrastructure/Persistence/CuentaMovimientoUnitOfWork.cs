using CuentaMovimientoService.Application.Exceptions;
using CuentaMovimientoService.Application.Ports;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CuentaMovimientoService.Infrastructure.Persistence;

public class CuentaMovimientoUnitOfWork : IUnitOfWork
{
    private readonly CuentaMovimientoDbContext _context;

    public CuentaMovimientoUnitOfWork(CuentaMovimientoDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            return await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx)
        {
            // Defensa en profundidad (EB-01/EB-03): el trigger trg_validar_saldo_ledger recalcula
            // saldo y cupo diario bajo FOR UPDATE para cerrar la carrera entre dos movimientos casi
            // simultáneos que ya pasaron los validadores de C# con datos obsoletos. Si el trigger
            // rechaza el INSERT, debe traducirse a la misma excepción de negocio que el validador
            // de aplicación, para que el middleware responda 400 con el mensaje exacto en vez de 500.
            throw TraducirExcepcionDeLedger(pgEx) ?? ex;
        }
    }

    private static Exception? TraducirExcepcionDeLedger(PostgresException pgEx)
    {
        if (pgEx.SqlState == PostgresErrorCodes.RaiseException &&
            pgEx.MessageText.Contains("Cupo diario Excedido", StringComparison.OrdinalIgnoreCase))
        {
            return new CupoDiarioExcedidoException();
        }

        if (pgEx.SqlState == PostgresErrorCodes.CheckViolation &&
            pgEx.ConstraintName == "CK_movimientos_saldo")
        {
            return new SaldoNoDisponibleException();
        }

        return null;
    }
}

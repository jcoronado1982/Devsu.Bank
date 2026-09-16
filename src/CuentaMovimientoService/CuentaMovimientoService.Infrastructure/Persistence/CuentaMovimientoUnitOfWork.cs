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

using CuentaMovimientoService.Application.Ports;
using CuentaMovimientoService.Domain.Entities;
using CuentaMovimientoService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CuentaMovimientoService.Infrastructure.Repositories;

public class MovimientoRepository : IMovimientoRepository
{
    private readonly CuentaMovimientoDbContext _context;

    public MovimientoRepository(CuentaMovimientoDbContext context)
    {
        _context = context;
    }

    public async Task<Movimiento?> ObtenerPorIdAsync(long movimientoId)
    {
        return await _context.Movimientos
            .Include(m => m.Cuenta)
            .FirstOrDefaultAsync(m => m.MovimientoId == movimientoId);
    }

    public async Task<IEnumerable<Movimiento>> ObtenerPorNumeroCuentaYFechaAsync(
        string numeroCuenta, DateTime desde, DateTime hasta)
    {
        var desdeUtc = desde.Kind == DateTimeKind.Utc ? desde : DateTime.SpecifyKind(desde, DateTimeKind.Utc);
        var hastaUtc = hasta.Kind == DateTimeKind.Utc ? hasta : DateTime.SpecifyKind(hasta, DateTimeKind.Utc);

        return await _context.Movimientos
            .Where(m => m.NumeroCuenta == numeroCuenta && m.Fecha >= desdeUtc && m.Fecha <= hastaUtc)
            .OrderByDescending(m => m.Fecha)
            .ToListAsync();
    }

    public async Task<decimal> ObtenerTotalRetiradoHoyAsync(string numeroCuenta)
    {
        var hoyInicioUtc = DateTime.UtcNow.Date;
        var hoyFinUtc = hoyInicioUtc.AddDays(1).AddTicks(-1);

        var total = await _context.Movimientos
            .Where(m => m.NumeroCuenta == numeroCuenta
                        && m.Fecha >= hoyInicioUtc
                        && m.Fecha <= hoyFinUtc
                        && m.Valor < 0)
            .SumAsync(m => (decimal?)m.Valor) ?? 0m;

        return Math.Abs(total);
    }

    public async Task AddAsync(Movimiento movimiento)
    {
        await _context.Movimientos.AddAsync(movimiento);
    }
}

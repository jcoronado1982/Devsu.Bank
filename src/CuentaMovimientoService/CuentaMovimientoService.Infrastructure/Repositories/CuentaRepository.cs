using CuentaMovimientoService.Application.Ports;
using CuentaMovimientoService.Domain.Entities;
using CuentaMovimientoService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CuentaMovimientoService.Infrastructure.Repositories;

// Las tres lecturas incluyen Movimientos y TipoCuentaItem siempre (nunca hay una versión
// "liviana"): Cuenta.ObtenerSaldoActual() necesita los movimientos cargados en memoria,
// y ReporteService/CuentaService.ToDto dependen de esas mismas consultas.
public class CuentaRepository : ICuentaRepository
{
    private readonly CuentaMovimientoDbContext _context;

    public CuentaRepository(CuentaMovimientoDbContext context)
    {
        _context = context;
    }

    public async Task<Cuenta?> ObtenerPorNumeroCuentaAsync(string numeroCuenta)
    {
        return await _context.Cuentas
            .Include(c => c.Movimientos)
            .Include(c => c.TipoCuentaItem)
            .FirstOrDefaultAsync(c => c.NumeroCuenta == numeroCuenta);
    }

    public async Task<IEnumerable<Cuenta>> ObtenerPorClienteIdAsync(long clienteId)
    {
        return await _context.Cuentas
            .Include(c => c.Movimientos)
            .Include(c => c.TipoCuentaItem)
            .Where(c => c.ClienteId == clienteId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Cuenta>> ObtenerTodasAsync()
    {
        return await _context.Cuentas
            .Include(c => c.Movimientos)
            .Include(c => c.TipoCuentaItem)
            .ToListAsync();
    }

    public async Task AddAsync(Cuenta cuenta)
    {
        await _context.Cuentas.AddAsync(cuenta);
    }

    public void Remove(Cuenta cuenta)
    {
        _context.Cuentas.Remove(cuenta);
    }
}

using CuentaMovimientoService.Application.Ports;
using CuentaMovimientoService.Domain.Entities;
using CuentaMovimientoService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CuentaMovimientoService.Infrastructure.Adapters;

public class ClienteExistsPort : IClienteExistsPort, IClienteInfoPort
{
    private readonly CuentaMovimientoDbContext _context;

    public ClienteExistsPort(CuentaMovimientoDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExisteClienteAsync(long clienteId)
    {
        return await _context.ClienteProyecciones.AnyAsync(c => c.ClienteId == clienteId && c.Estado);
    }

    public async Task<string?> ObtenerNombreClienteAsync(long clienteId)
    {
        var proy = await _context.ClienteProyecciones.FirstOrDefaultAsync(c => c.ClienteId == clienteId);
        return proy?.Nombre;
    }

    public async Task<long?> ObtenerClienteIdPorNombreAsync(string nombre)
    {
        var proy = await _context.ClienteProyecciones
            .FirstOrDefaultAsync(c => EF.Functions.ILike(c.Nombre, nombre.Trim()));
        return proy?.ClienteId;
    }

    public async Task<long?> ObtenerClienteIdPorIdentificacionAsync(string identificacion)
    {
        var texto = identificacion.Trim();
        var proy = await _context.ClienteProyecciones
            .FirstOrDefaultAsync(c => c.Identificacion == texto);
        return proy?.ClienteId;
    }

    public async Task<IReadOnlyList<long>> ObtenerClienteIdsPorNombreParcialAsync(string nombreParcial)
    {
        var texto = nombreParcial.Trim();
        return await _context.ClienteProyecciones
            .Where(c => EF.Functions.ILike(c.Nombre, $"%{texto}%"))
            .Select(c => c.ClienteId)
            .ToListAsync();
    }

    public async Task GuardarProyeccionAsync(ClienteProyeccion proyeccion)
    {
        var existente = await _context.ClienteProyecciones.FirstOrDefaultAsync(c => c.ClienteId == proyeccion.ClienteId);
        if (existente is null)
        {
            await _context.ClienteProyecciones.AddAsync(proyeccion);
        }
        else
        {
            existente.Actualizar(proyeccion.Nombre, proyeccion.Identificacion, proyeccion.Estado);
        }
        await _context.SaveChangesAsync();
    }

    public async Task DesactivarProyeccionAsync(long clienteId)
    {
        var existente = await _context.ClienteProyecciones.FirstOrDefaultAsync(c => c.ClienteId == clienteId);
        if (existente is null)
        {
            return;
        }

        existente.Actualizar(existente.Nombre, existente.Identificacion, false);
        await _context.SaveChangesAsync();
    }
}

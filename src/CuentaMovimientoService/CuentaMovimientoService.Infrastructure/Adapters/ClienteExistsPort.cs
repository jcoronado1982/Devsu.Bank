using CuentaMovimientoService.Application.Ports;
using CuentaMovimientoService.Domain.Entities;
using CuentaMovimientoService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CuentaMovimientoService.Infrastructure.Adapters;

// Implementa ambos puertos de lectura de cliente sobre la misma tabla de proyección
// (ClienteProyecciones), porque los dos leen la misma fuente sin necesidad de duplicar el DbContext.
public class ClienteExistsPort : IClienteExistsPort, IClienteInfoPort
{
    private readonly CuentaMovimientoDbContext _context;

    public ClienteExistsPort(CuentaMovimientoDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExisteClienteAsync(long clienteId)
    {
        // Un cliente eliminado (soft-delete via ClienteEliminadoEvent, Estado=false) no debe
        // permitir abrir cuentas nuevas, aunque su proyección se conserve por integridad
        // referencial con cuentas/movimientos históricos.
        return await _context.ClienteProyecciones.AnyAsync(c => c.ClienteId == clienteId && c.Estado);
    }

    public async Task<string?> ObtenerNombreClienteAsync(long clienteId)
    {
        var proy = await _context.ClienteProyecciones.FirstOrDefaultAsync(c => c.ClienteId == clienteId);
        return proy?.Nombre;
    }

    // Match EXACTO (case-insensitive vía ILIKE, sin comodines). Usado como resolución
    // legacy de un único cliente por nombre completo; ReporteService ya no depende de este
    // método para /reportes (usa el parcial de abajo), pero se mantiene por compatibilidad.
    public async Task<long?> ObtenerClienteIdPorNombreAsync(string nombre)
    {
        var proy = await _context.ClienteProyecciones
            .FirstOrDefaultAsync(c => EF.Functions.ILike(c.Nombre, nombre.Trim()));
        return proy?.ClienteId;
    }

    // Identificación/documento es único por cliente, por eso basta un match exacto
    // (no requiere manejo de ambigüedad como el nombre).
    public async Task<long?> ObtenerClienteIdPorIdentificacionAsync(string identificacion)
    {
        var texto = identificacion.Trim();
        var proy = await _context.ClienteProyecciones
            .FirstOrDefaultAsync(c => c.Identificacion == texto);
        return proy?.ClienteId;
    }

    // Búsqueda por nombre "como en la vida real": parcial (ILIKE %texto%) y case-insensitive,
    // para no exigirle al usuario el nombre completo exacto. Puede devolver 0, 1 o varios
    // clientes; ReporteService concatena el reporte de todos los que matcheen en vez de elegir
    // uno al azar o fallar (ambigüedad de nombre != cliente inexistente).
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

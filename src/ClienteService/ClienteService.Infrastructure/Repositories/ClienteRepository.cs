using ClienteService.Application.Ports;
using ClienteService.Domain.Entities;
using ClienteService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClienteService.Infrastructure.Repositories;

/// <summary>
/// Implementación EF Core de IClienteRepository sobre PostgreSQL. Ninguno de estos métodos
/// llama a SaveChanges: AddAsync/Remove solo marcan cambios en el ChangeTracker, y es
/// ClienteUnitOfWork.SaveChangesAsync (invocado por ClienteService) quien confirma la
/// transacción, manteniendo el patrón Unit of Work correctamente separado del Repository.
/// </summary>
public class ClienteRepository : IClienteRepository
{
    private readonly ClienteDbContext _context;

    public ClienteRepository(ClienteDbContext context)
    {
        _context = context;
    }

    public async Task<Cliente?> ObtenerPorIdAsync(long clienteId)
    {
        return await _context.Clientes.FirstOrDefaultAsync(c => c.PersonaId == clienteId);
    }

    public async Task<Cliente?> ObtenerPorIdentificacionAsync(string identificacion)
    {
        // Persona normaliza la identificación a mayúsculas al crearse; se normaliza también
        // aquí para que la búsqueda de duplicados no dependa de la mayúscula/minúscula recibida
        // y siga usando el índice único sobre la columna sin envolverla en una función.
        var identificacionNormalizada = identificacion?.Trim().ToUpperInvariant() ?? string.Empty;
        return await _context.Clientes.FirstOrDefaultAsync(c => c.Identificacion == identificacionNormalizada);
    }

    public async Task<IEnumerable<Cliente>> ObtenerTodosAsync()
    {
        return await _context.Clientes.ToListAsync();
    }

    public async Task AddAsync(Cliente cliente)
    {
        await _context.Clientes.AddAsync(cliente);
    }

    public void Remove(Cliente cliente)
    {
        _context.Clientes.Remove(cliente);
    }
}

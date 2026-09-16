using ClienteService.Application.Ports;
using ClienteService.Domain.Entities;
using ClienteService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClienteService.Infrastructure.Repositories;

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

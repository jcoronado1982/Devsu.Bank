using ClienteService.Domain.Entities;
namespace ClienteService.Application.Ports;
public interface IClienteRepository
{
    Task<Cliente?> ObtenerPorIdAsync(long clienteId);
    Task<Cliente?> ObtenerPorIdentificacionAsync(string identificacion);
    Task<IEnumerable<Cliente>> ObtenerTodosAsync();
    Task AddAsync(Cliente cliente);
    void Remove(Cliente cliente);
}

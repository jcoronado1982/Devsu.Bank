using ClienteService.Domain.Entities;
namespace ClienteService.Application.Ports;

/// <summary>
/// Puerto de persistencia (patrón Repository) para la entidad Cliente. La capa Application
/// depende únicamente de esta interfaz; ClienteRepository (Infrastructure) la implementa con
/// EF Core + PostgreSQL, cumpliendo la regla de "cero dependencias de EF Core en Application".
/// Ninguno de estos métodos hace commit: la persistencia efectiva ocurre al invocar
/// IUnitOfWork.SaveChangesAsync desde ClienteService.
/// </summary>
public interface IClienteRepository
{
    Task<Cliente?> ObtenerPorIdAsync(long clienteId);
    Task<Cliente?> ObtenerPorIdentificacionAsync(string identificacion);
    Task<IEnumerable<Cliente>> ObtenerTodosAsync();
    Task AddAsync(Cliente cliente);
    void Remove(Cliente cliente);
}

using ClienteService.Application.DTOs;
namespace ClienteService.Application.Services;

/// <summary>
/// Puerto/facade de casos de uso de Cliente consumido por ClientesController. Orquesta
/// validación de negocio, persistencia (vía IClienteRepository/IUnitOfWork) y publicación de
/// eventos de dominio (vía IEventBus); el controlador no conoce ninguno de esos detalles.
/// </summary>
public interface IClienteService
{
    Task<ClienteDto> CrearClienteAsync(CrearClienteDto dto);
    Task<ClienteDto?> ObtenerClientePorIdAsync(long clienteId);
    Task<IEnumerable<ClienteDto>> ObtenerTodosLosClientesAsync();
    Task<ClienteDto> ActualizarClienteAsync(long clienteId, ActualizarClienteDto dto);
    Task EliminarClienteAsync(long clienteId);
}

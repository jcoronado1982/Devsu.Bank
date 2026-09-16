using ClienteService.Application.DTOs;
namespace ClienteService.Application.Services;

public interface IClienteService
{
    Task<ClienteDto> CrearClienteAsync(CrearClienteDto dto);
    Task<ClienteDto?> ObtenerClientePorIdAsync(long clienteId);
    Task<IEnumerable<ClienteDto>> ObtenerTodosLosClientesAsync();
    Task<ClienteDto> ActualizarClienteAsync(long clienteId, ActualizarClienteDto dto);
    Task<ClienteDto> ActualizarClienteParcialAsync(long clienteId, ActualizarClienteParcialDto dto);
    Task EliminarClienteAsync(long clienteId);
}

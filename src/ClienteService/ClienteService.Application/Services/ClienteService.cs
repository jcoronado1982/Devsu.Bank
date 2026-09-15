using ClienteService.Application.DTOs;
using ClienteService.Application.Events;
using ClienteService.Application.Exceptions;
using ClienteService.Application.Ports;
using ClienteService.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace ClienteService.Application.Services;

public class ClienteService : IClienteService
{
    private readonly IClienteRepository _repo;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventBus? _eventBus;
    private readonly ILogger<ClienteService>? _logger;

    public ClienteService(
        IClienteRepository repo,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        IEventBus? eventBus = null,
        ILogger<ClienteService>? logger = null)
    {
        _repo = repo;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<ClienteDto> CrearClienteAsync(CrearClienteDto dto)
    {
        _logger?.LogInformation("Iniciando creación de cliente con identificación {Identificacion}", dto.Identificacion);

        var existente = await _repo.ObtenerPorIdentificacionAsync(dto.Identificacion);
        if (existente is not null)
        {
            _logger?.LogWarning("Identificación duplicada rechazada: {Identificacion}", dto.Identificacion);
            throw new IdentificacionDuplicadaException(dto.Identificacion);
        }

        var contrasenaHash = _passwordHasher.HashPassword(dto.Contrasena);
        var cliente = new Cliente(
            dto.Nombre,
            dto.Genero,
            dto.Edad,
            dto.Identificacion,
            dto.Direccion,
            dto.Telefono,
            contrasenaHash,
            dto.Estado);

        await _repo.AddAsync(cliente);
        await _unitOfWork.SaveChangesAsync();

        _logger?.LogInformation("Cliente creado exitosamente con ID {ClienteId}", cliente.ClienteId);

        if (_eventBus is not null)
        {
            try
            {
                var evento = new ClienteCreadoEvent(
                    cliente.ClienteId,
                    cliente.Nombre,
                    cliente.Identificacion,
                    cliente.Direccion,
                    cliente.Telefono,
                    cliente.Estado);

                await _eventBus.PublicarAsync(evento);
                _logger?.LogInformation("Evento ClienteCreadoEvent publicado para cliente {ClienteId}", cliente.ClienteId);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "No se pudo publicar ClienteCreadoEvent en el broker de mensajería");
            }
        }

        return ToDto(cliente);
    }

    public async Task<ClienteDto?> ObtenerClientePorIdAsync(long clienteId)
    {
        var cliente = await _repo.ObtenerPorIdAsync(clienteId);
        return cliente is null ? null : ToDto(cliente);
    }

    public async Task<IEnumerable<ClienteDto>> ObtenerTodosLosClientesAsync()
    {
        var lista = await _repo.ObtenerTodosAsync();
        return lista.Select(ToDto);
    }

    public async Task<ClienteDto> ActualizarClienteAsync(long clienteId, ActualizarClienteDto dto)
    {
        _logger?.LogInformation("Actualizando cliente con ID {ClienteId}", clienteId);

        var cliente = await _repo.ObtenerPorIdAsync(clienteId)
                      ?? throw new ClienteNotFoundException(clienteId);

        // La identificación es inmutable (DICCIONARIO_DE_DATOS_Y_TIPOS.md): no se acepta en el DTO de actualización.
        cliente.ActualizarDatosPersona(
            dto.Nombre,
            dto.Genero,
            dto.Edad,
            dto.Direccion,
            dto.Telefono);

        if (!string.IsNullOrWhiteSpace(dto.Contrasena))
        {
            cliente.CambiarContrasena(_passwordHasher.HashPassword(dto.Contrasena));
        }

        if (dto.Estado)
            cliente.Activar();
        else
            cliente.Inactivar();

        await _unitOfWork.SaveChangesAsync();

        _logger?.LogInformation("Cliente con ID {ClienteId} actualizado exitosamente", clienteId);

        return ToDto(cliente);
    }

    public async Task EliminarClienteAsync(long clienteId)
    {
        _logger?.LogInformation("Eliminando cliente con ID {ClienteId}", clienteId);

        var cliente = await _repo.ObtenerPorIdAsync(clienteId)
                      ?? throw new ClienteNotFoundException(clienteId);

        _repo.Remove(cliente);
        await _unitOfWork.SaveChangesAsync();

        _logger?.LogInformation("Cliente con ID {ClienteId} eliminado exitosamente", clienteId);

        if (_eventBus is not null)
        {
            try
            {
                await _eventBus.PublicarAsync(new ClienteEliminadoEvent(clienteId));
                _logger?.LogInformation("Evento ClienteEliminadoEvent publicado para cliente {ClienteId}", clienteId);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "No se pudo publicar ClienteEliminadoEvent en el broker de mensajería");
            }
        }
    }

    private static ClienteDto ToDto(Cliente c) => new(
        c.PersonaId,
        c.Nombre,
        c.Genero,
        c.Edad,
        c.Identificacion,
        c.Direccion,
        c.Telefono,
        c.Contrasena,
        c.Estado);
}

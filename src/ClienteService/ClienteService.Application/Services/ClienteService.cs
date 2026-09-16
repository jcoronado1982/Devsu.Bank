using ClienteService.Application.DTOs;
using Devsu.Contracts.Events;
using ClienteService.Application.Exceptions;
using ClienteService.Application.Ports;
using ClienteService.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace ClienteService.Application.Services;

/// <summary>
/// Implementación única del caso de uso de Cliente: orquesta validación de duplicados,
/// hasheo de contraseña, persistencia transaccional y publicación de eventos de integración.
/// IEventBus, ILogger y ICuentaExistsPort son opcionales (nulables) para que la clase también
/// pueda instanciarse en pruebas unitarias sin necesidad de infraestructura de mensajería,
/// logging ni HTTP; en producción, Program.cs los registra todos.
/// </summary>
public class ClienteService : IClienteService
{
    private readonly IClienteRepository _repo;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventBus? _eventBus;
    private readonly ILogger<ClienteService>? _logger;
    private readonly ICuentaExistsPort? _cuentaExistsPort;

    public ClienteService(
        IClienteRepository repo,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        IEventBus? eventBus = null,
        ILogger<ClienteService>? logger = null,
        ICuentaExistsPort? cuentaExistsPort = null)
    {
        _repo = repo;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _eventBus = eventBus;
        _logger = logger;
        _cuentaExistsPort = cuentaExistsPort;
    }

    /// <summary>
    /// Crea un cliente nuevo. Rechaza identificaciones duplicadas (EB-06, HTTP 409) antes de
    /// tocar la base de datos; ClienteUnitOfWork añade una segunda defensa contra la carrera
    /// de dos creaciones simultáneas con la misma identificación. La contraseña se hashea con
    /// BCrypt antes de construir la entidad: el texto plano del DTO nunca se persiste.
    /// La publicación del evento ClienteCreadoEvent es "best effort": si el broker falla, se
    /// registra un warning pero la creación del cliente igual se considera exitosa (no hay
    /// outbox transaccional en esta versión).
    /// </summary>
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

    /// <summary>
    /// Devuelve null si no existe el cliente; a diferencia de Actualizar/Eliminar, no lanza
    /// ClienteNotFoundException aquí, para que el controlador decida cómo responder el 404.
    /// </summary>
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

    /// <summary>
    /// Actualiza los datos mutables de un cliente existente (EB-07: HTTP 404 "Cliente no
    /// encontrado" si el ID no existe). La contraseña solo se re-hashea y reemplaza si el DTO
    /// trae un valor no vacío; en caso contrario se conserva la contraseña actual sin cambios.
    /// </summary>
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

    /// <summary>
    /// Elimina un cliente, pero solo si CuentaMovimientoService confirma que no tiene cuentas
    /// asociadas (se consulta vía la única llamada HTTP síncrona entre microservicios que
    /// permite este proyecto, ICuentaExistsPort). Si ese puerto no está configurado
    /// (_cuentaExistsPort null, p. ej. en pruebas), la verificación simplemente se omite.
    /// </summary>
    public async Task EliminarClienteAsync(long clienteId)
    {
        _logger?.LogInformation("Eliminando cliente con ID {ClienteId}", clienteId);

        var cliente = await _repo.ObtenerPorIdAsync(clienteId)
                      ?? throw new ClienteNotFoundException(clienteId);

        if (_cuentaExistsPort is not null && await _cuentaExistsPort.ClienteTieneCuentasAsync(clienteId))
        {
            _logger?.LogWarning("Eliminación rechazada: cliente {ClienteId} tiene cuentas asociadas", clienteId);
            throw new ClienteConCuentasAsociadasException(clienteId);
        }

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

    // Mapeo interno Cliente -> ClienteDto: incluye el hash de Contrasena porque ClienteDto es
    // un DTO de uso interno de la capa Application, no el contrato público de la API (ese rol
    // lo cumple ClienteResponseDto, que el controlador construye sin este campo).
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

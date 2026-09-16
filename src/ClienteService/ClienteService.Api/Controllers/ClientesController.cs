using ClienteService.Application.DTOs;
using ClienteService.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClienteService.Api.Controllers;

/// <summary>
/// Controlador delgado (Thin Controller) de /clientes: implementa el CRUD completo exigido
/// para la entidad Cliente (F1). No contiene lógica de negocio ni llama a SaveChanges; solo
/// traduce HTTP <-> DTOs y delega en IClienteService. Toda excepción de negocio lanzada por
/// el servicio se deja propagar sin capturarla aquí: GlobalExceptionMiddleware es quien la
/// traduce al código de estado HTTP correspondiente.
/// </summary>
[ApiController]
[Route("clientes")]
public class ClientesController : ControllerBase
{
    private readonly IClienteService _clienteService;
    private readonly ILogger<ClientesController> _logger;

    public ClientesController(IClienteService clienteService, ILogger<ClientesController> logger)
    {
        _clienteService = clienteService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClienteResponseDto>>> ObtenerTodos()
    {
        _logger.LogInformation("Consultando todos los clientes");
        var clientes = await _clienteService.ObtenerTodosLosClientesAsync();
        return Ok(clientes.Select(ToResponse));
    }

    /// <summary>
    /// GET /clientes/{id}. Único punto donde el controlador mismo decide el 404 (en vez de
    /// delegarlo a una excepción vía GlobalExceptionMiddleware), porque IClienteService.
    /// ObtenerClientePorIdAsync devuelve null en lugar de lanzar ClienteNotFoundException.
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ActionResult<ClienteResponseDto>> ObtenerPorId(long id)
    {
        _logger.LogInformation("Consultando cliente por ID {ClienteId}", id);
        var cliente = await _clienteService.ObtenerClientePorIdAsync(id);
        if (cliente is null)
        {
            _logger.LogWarning("Cliente con ID {ClienteId} no encontrado", id);
            return NotFound(new { mensaje = "Cliente no encontrado" });
        }

        return Ok(ToResponse(cliente));
    }

    /// <summary>
    /// POST /clientes. Responde 201 Created con el header Location apuntando a
    /// GET /clientes/{id}, siguiendo la convención REST estándar para creación de recursos.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ClienteResponseDto>> Crear([FromBody] CrearClienteDto dto)
    {
        _logger.LogInformation("Recibida solicitud para crear cliente con identificación {Identificacion}", dto.Identificacion);
        var cliente = await _clienteService.CrearClienteAsync(dto);
        var response = ToResponse(cliente);
        return CreatedAtAction(nameof(ObtenerPorId), new { id = cliente.ClienteId }, response);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<ClienteResponseDto>> Actualizar(long id, [FromBody] ActualizarClienteDto dto)
    {
        _logger.LogInformation("Recibida solicitud para actualizar cliente con ID {ClienteId}", id);
        var cliente = await _clienteService.ActualizarClienteAsync(id, dto);
        return Ok(ToResponse(cliente));
    }

    // PATCH reutiliza exactamente la misma implementación que PUT (ActualizarClienteAsync
    // exige siempre el DTO completo): este servicio no ofrece una semántica de actualización
    // parcial de campo por campo, solo dos rutas HTTP equivalentes hacia el mismo caso de uso.
    [HttpPatch("{id:long}")]
    public async Task<ActionResult<ClienteResponseDto>> ActualizarParcial(long id, [FromBody] ActualizarClienteDto dto)
    {
        _logger.LogInformation("Recibida solicitud PATCH para cliente con ID {ClienteId}", id);
        var cliente = await _clienteService.ActualizarClienteAsync(id, dto);
        return Ok(ToResponse(cliente));
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Eliminar(long id)
    {
        _logger.LogInformation("Recibida solicitud para eliminar cliente con ID {ClienteId}", id);
        await _clienteService.EliminarClienteAsync(id);
        return NoContent();
    }

    // Mapeo de salida ClienteDto -> ClienteResponseDto: omite deliberadamente c.Contrasena.
    // Es el punto exacto donde se cumple la regla OWASP de no exponer contraseñas (ni
    // siquiera hasheadas) en ningún endpoint GET/POST/PUT/PATCH de este controlador.
    private static ClienteResponseDto ToResponse(ClienteDto c) => new(
        c.ClienteId,
        c.Nombre,
        c.Genero,
        c.Edad,
        c.Identificacion,
        c.Direccion,
        c.Telefono,
        c.Estado
    );
}

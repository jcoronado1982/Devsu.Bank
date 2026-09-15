using ClienteService.Application.DTOs;
using ClienteService.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClienteService.Api.Controllers;

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

using CuentaMovimientoService.Application.DTOs;
using CuentaMovimientoService.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace CuentaMovimientoService.Api.Controllers;

[ApiController]
[Route("movimientos")]
public class MovimientosController : ControllerBase
{
    private readonly IMovimientoService _movimientoService;
    private readonly ILogger<MovimientosController> _logger;

    public MovimientosController(
        IMovimientoService movimientoService,
        ILogger<MovimientosController> logger)
    {
        _movimientoService = movimientoService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<MovimientoDto>> Registrar([FromBody] RegistrarMovimientoDto dto)
    {
        _logger.LogInformation("Recibida solicitud de movimiento en cuenta {NumeroCuenta} por valor {Valor}",
            dto.NumeroCuenta, dto.Valor);

        var movimiento = await _movimientoService.RegistrarMovimientoAsync(dto);
        return StatusCode(StatusCodes.Status201Created, movimiento);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<MovimientoDto>> ObtenerPorId(long id)
    {
        _logger.LogInformation("Consultando movimiento ID {MovimientoId}", id);
        var mov = await _movimientoService.ObtenerPorIdAsync(id);
        if (mov is null)
        {
            return NotFound(new { mensaje = "Movimiento no encontrado" });
        }

        return Ok(mov);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MovimientoDto>>> ObtenerPorFiltro(
        [FromQuery] string? numeroCuenta,
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta)
    {
        if (string.IsNullOrWhiteSpace(numeroCuenta))
        {
            return BadRequest(new { mensaje = "El número de cuenta es obligatorio para consultar movimientos" });
        }

        var fDesde = desde ?? DateTime.MinValue;
        var fHasta = hasta ?? DateTime.MaxValue;

        var resultado = await _movimientoService.ObtenerPorFiltroAsync(numeroCuenta, fDesde, fHasta);
        return Ok(resultado);
    }
}

using CuentaMovimientoService.Application.DTOs;
using CuentaMovimientoService.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace CuentaMovimientoService.Api.Controllers;

[ApiController]
[Route("reportes")]
public class ReportesController : ControllerBase
{
    private readonly IReporteService _reporteService;
    private readonly ILogger<ReportesController> _logger;

    public ReportesController(IReporteService reporteService, ILogger<ReportesController> logger)
    {
        _reporteService = reporteService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerReporte(
        [FromQuery] string? fecha,
        [FromQuery] string? cliente)
    {
        _logger.LogInformation("Solicitud de reporte con parámetros fecha: '{Fecha}', cliente: '{Cliente}'", fecha, cliente);

        if (string.IsNullOrWhiteSpace(cliente))
        {
            return BadRequest(new { mensaje = "El parámetro 'cliente' es obligatorio" });
        }

        var items = await _reporteService.GenerarReporteEstadoCuentaAsync(cliente, fecha);
        var respuesta = items.Select(ReporteResponseItemDto.DesdeReporteMovimiento).ToList();

        return Ok(respuesta);
    }
}

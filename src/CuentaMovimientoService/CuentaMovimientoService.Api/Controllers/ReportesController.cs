using CuentaMovimientoService.Application.DTOs;
using CuentaMovimientoService.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace CuentaMovimientoService.Api.Controllers;

// F4 de OBJETIVO.md: /reportes?fecha=rango&cliente=. "cliente" resuelve en cascada dentro de
// ReporteService (id interno -> identificación exacta -> nombre parcial); si matchea a varios
// clientes por nombre, la respuesta trae a todos agrupados (nunca se mezclan sus movimientos
// en una sola fila plana). Controlador delgado: solo valida presencia de "cliente" y agrupa
// el resultado para la respuesta, sin lógica de negocio.
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
        var respuesta = ReporteClienteResponseDto.AgruparDesdeMovimientos(items);

        return Ok(respuesta);
    }
}

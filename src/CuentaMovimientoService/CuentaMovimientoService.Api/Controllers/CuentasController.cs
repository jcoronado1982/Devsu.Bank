using CuentaMovimientoService.Application.DTOs;
using CuentaMovimientoService.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace CuentaMovimientoService.Api.Controllers;

[ApiController]
[Route("cuentas")]
public class CuentasController : ControllerBase
{
    private readonly ICuentaService _cuentaService;
    private readonly ILogger<CuentasController> _logger;

    public CuentasController(ICuentaService cuentaService, ILogger<CuentasController> logger)
    {
        _cuentaService = cuentaService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CuentaDto>>> ObtenerTodas([FromQuery] long? clienteId)
    {
        if (clienteId.HasValue)
        {
            _logger.LogInformation("Consultando cuentas para cliente ID {ClienteId}", clienteId.Value);
            var cuentasCliente = await _cuentaService.ObtenerCuentasPorClienteAsync(clienteId.Value);
            return Ok(cuentasCliente);
        }

        _logger.LogInformation("Consultando todas las cuentas registradas");
        var cuentas = await _cuentaService.ObtenerTodasAsync();
        return Ok(cuentas);
    }

    [HttpGet("{numeroCuenta}")]
    public async Task<ActionResult<CuentaDto>> ObtenerPorNumero(string numeroCuenta)
    {
        _logger.LogInformation("Consultando cuenta {NumeroCuenta}", numeroCuenta);
        var cuenta = await _cuentaService.ObtenerCuentaAsync(numeroCuenta);
        if (cuenta is null)
        {
            _logger.LogWarning("Cuenta {NumeroCuenta} no encontrada", numeroCuenta);
            return NotFound(new { mensaje = "Cuenta no encontrada" });
        }

        return Ok(cuenta);
    }

    [HttpPost]
    public async Task<ActionResult<CuentaDto>> Crear([FromBody] CrearCuentaDto dto)
    {
        _logger.LogInformation("Recibida solicitud para abrir cuenta {NumeroCuenta} para cliente ID {ClienteId}", dto.NumeroCuenta, dto.ClienteId);
        var cuenta = await _cuentaService.CrearCuentaAsync(dto);
        return CreatedAtAction(nameof(ObtenerPorNumero), new { numeroCuenta = cuenta.NumeroCuenta }, cuenta);
    }

    [HttpPut("{numeroCuenta}")]
    public async Task<ActionResult<CuentaDto>> Actualizar(string numeroCuenta, [FromBody] ActualizarCuentaDto dto)
    {
        _logger.LogInformation("Recibida solicitud para actualizar cuenta {NumeroCuenta}", numeroCuenta);
        var cuenta = await _cuentaService.ActualizarCuentaAsync(numeroCuenta, dto);
        return Ok(cuenta);
    }

    [HttpPatch("{numeroCuenta}")]
    public async Task<ActionResult<CuentaDto>> ActualizarParcial(string numeroCuenta, [FromBody] ActualizarCuentaDto dto)
    {
        _logger.LogInformation("Recibida solicitud PATCH para cuenta {NumeroCuenta}", numeroCuenta);
        var cuenta = await _cuentaService.ActualizarCuentaAsync(numeroCuenta, dto);
        return Ok(cuenta);
    }
}

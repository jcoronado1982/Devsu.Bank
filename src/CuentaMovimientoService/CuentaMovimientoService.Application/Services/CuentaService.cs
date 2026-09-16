using CuentaMovimientoService.Application.DTOs;
using CuentaMovimientoService.Application.Exceptions;
using CuentaMovimientoService.Application.Ports;
using CuentaMovimientoService.Domain.Entities;
using CuentaMovimientoService.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CuentaMovimientoService.Application.Services;

// Orquesta F1 (CRU de Cuenta) validando la existencia del cliente dueño (EB-07) y el
// número de cuenta único (409 Conflict) antes de persistir. No valida reglas de movimientos
// (EB-01/03/04/05) — esas viven en MovimientoService/IMovimientoValidator.
public class CuentaService : ICuentaService
{
    private readonly ICuentaRepository _repo;
    private readonly IClienteExistsPort _clientePort;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CuentaService>? _logger;

    public CuentaService(
        ICuentaRepository repo,
        IClienteExistsPort clientePort,
        IUnitOfWork unitOfWork,
        ILogger<CuentaService>? logger = null)
    {
        _repo = repo;
        _clientePort = clientePort;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CuentaDto> CrearCuentaAsync(CrearCuentaDto dto)
    {
        _logger?.LogInformation("Creando cuenta bancaria {NumeroCuenta} para cliente ID {ClienteId}", dto.NumeroCuenta, dto.ClienteId);

        if (!await _clientePort.ExisteClienteAsync(dto.ClienteId))
        {
            _logger?.LogWarning("Cliente ID {ClienteId} no encontrado (EB-07)", dto.ClienteId);
            throw new ClienteNoEncontradoException(dto.ClienteId);
        }

        if (dto.SaldoInicial < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dto.SaldoInicial), "El saldo inicial no puede ser negativo.");
        }

        if (await _repo.ObtenerPorNumeroCuentaAsync(dto.NumeroCuenta) is not null)
        {
            _logger?.LogWarning("Número de cuenta duplicado rechazado: {NumeroCuenta}", dto.NumeroCuenta);
            throw new NumeroCuentaDuplicadoException(dto.NumeroCuenta);
        }

        var tipoCuenta = dto.TipoCuenta?.Trim().ToLowerInvariant() switch
        {
            "ahorros" => TipoCuenta.Ahorros,
            "corriente" => TipoCuenta.Corriente,
            _ => throw new ArgumentException("El tipo de cuenta debe ser 'Ahorros' o 'Corriente'.", nameof(dto.TipoCuenta))
        };

        var cuenta = new Cuenta(dto.NumeroCuenta, tipoCuenta, dto.SaldoInicial, dto.ClienteId, dto.Estado);
        await _repo.AddAsync(cuenta);
        await _unitOfWork.SaveChangesAsync();

        _logger?.LogInformation("Cuenta {NumeroCuenta} creada exitosamente con saldo inicial {SaldoInicial}", cuenta.NumeroCuenta, cuenta.SaldoInicial);

        return ToDto(cuenta);
    }

    public async Task<CuentaDto?> ObtenerCuentaAsync(string numeroCuenta)
    {
        var cuenta = await _repo.ObtenerPorNumeroCuentaAsync(numeroCuenta);
        return cuenta is null ? null : ToDto(cuenta);
    }

    public async Task<CuentaDto> ActualizarCuentaAsync(string numeroCuenta, ActualizarCuentaDto dto)
    {
        _logger?.LogInformation("Actualizando cuenta {NumeroCuenta}", numeroCuenta);

        var cuenta = await _repo.ObtenerPorNumeroCuentaAsync(numeroCuenta)
                     ?? throw new CuentaNotFoundException(numeroCuenta);

        if (dto.Estado)
            cuenta.Activar();
        else
            cuenta.Inactivar();

        await _unitOfWork.SaveChangesAsync();

        _logger?.LogInformation("Cuenta {NumeroCuenta} actualizada exitosamente (Estado: {Estado})", numeroCuenta, cuenta.Estado);

        return ToDto(cuenta);
    }

    public async Task<IEnumerable<CuentaDto>> ObtenerCuentasPorClienteAsync(long clienteId)
    {
        var lista = await _repo.ObtenerPorClienteIdAsync(clienteId);
        return lista.Select(ToDto);
    }

    public async Task<IEnumerable<CuentaDto>> ObtenerTodasAsync()
    {
        var lista = await _repo.ObtenerTodasAsync();
        return lista.Select(ToDto);
    }

    private static CuentaDto ToDto(Cuenta c) => new(
        c.NumeroCuenta,
        c.TipoCuentaId == 2 ? "Corriente" : "Ahorros",
        c.SaldoInicial,
        c.ObtenerSaldoActual(),
        c.Estado,
        c.ClienteId);
}

using System.Linq;
using CuentaMovimientoService.Application.DTOs;
using CuentaMovimientoService.Application.Exceptions;
using CuentaMovimientoService.Application.Ports;
using CuentaMovimientoService.Application.Validators;
using CuentaMovimientoService.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CuentaMovimientoService.Application.Services;

public class MovimientoService : IMovimientoService
{
    private readonly ICuentaRepository _cuentaRepo;
    private readonly IMovimientoRepository _movimientoRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MovimientoService>? _logger;
    private readonly IReadOnlyList<IMovimientoValidator> _validators;

    public MovimientoService(
        ICuentaRepository cuentaRepo,
        IMovimientoRepository movimientoRepo,
        IUnitOfWork unitOfWork,
        ILogger<MovimientoService>? logger = null,
        IEnumerable<IMovimientoValidator>? validators = null)
    {
        _cuentaRepo = cuentaRepo;
        _movimientoRepo = movimientoRepo;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _validators = (validators ?? DefaultValidators()).ToList();
    }

    private static IEnumerable<IMovimientoValidator> DefaultValidators() =>
    [
        new CuentaActivaValidator(),
        new SaldoSuficienteValidator(),
        new CupoDiarioValidator()
    ];

    public async Task<MovimientoDto> RegistrarMovimientoAsync(RegistrarMovimientoDto dto)
    {
        // EB-05: Movimiento con valor 0.00 rechazado (guard clause, no es una regla de negocio del ledger)
        if (dto.Valor == 0m)
        {
            _logger?.LogWarning("Intento de registrar movimiento con valor cero en cuenta {NumeroCuenta}", dto.NumeroCuenta);
            throw new ArgumentException("El valor del movimiento no puede ser cero.", nameof(dto.Valor));
        }

        var cuenta = await _cuentaRepo.ObtenerPorNumeroCuentaAsync(dto.NumeroCuenta)
                     ?? throw new CuentaNotFoundException(dto.NumeroCuenta);

        // EB-04/EB-01/EB-03: reglas de negocio del ledger, una clase Strategy por regla (Open/Closed)
        foreach (var validador in _validators)
        {
            await validador.ValidarAsync(cuenta, dto.Valor, _movimientoRepo);
        }

        var saldoActual = cuenta.ObtenerSaldoActual();
        var saldoResultante = decimal.Round(saldoActual + dto.Valor, 2, MidpointRounding.AwayFromZero);
        var tipo = dto.Valor > 0 ? "Deposito" : "Retiro";

        var movimiento = new Movimiento(DateTime.UtcNow, tipo, dto.Valor, saldoResultante, dto.NumeroCuenta);
        await _movimientoRepo.AddAsync(movimiento);
        await _unitOfWork.SaveChangesAsync();

        _logger?.LogInformation("Movimiento {Tipo} registrado exitosamente en cuenta {NumeroCuenta} con valor {Valor}. Saldo nuevo: {Saldo}",
            tipo, dto.NumeroCuenta, dto.Valor, saldoResultante);

        return ToDto(movimiento);
    }

    public async Task<MovimientoDto?> ObtenerPorIdAsync(long movimientoId)
    {
        var movimiento = await _movimientoRepo.ObtenerPorIdAsync(movimientoId);
        return movimiento is null ? null : ToDto(movimiento);
    }

    public async Task<IEnumerable<MovimientoDto>> ObtenerPorFiltroAsync(string numeroCuenta, DateTime desde, DateTime hasta)
    {
        var movimientos = await _movimientoRepo.ObtenerPorNumeroCuentaYFechaAsync(numeroCuenta, desde, hasta);
        return movimientos.Select(ToDto);
    }

    private static MovimientoDto ToDto(Movimiento m) => new(
        m.MovimientoId,
        m.Fecha,
        m.TipoMovimiento,
        m.Valor,
        m.Saldo,
        m.NumeroCuenta);
}

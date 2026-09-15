using CuentaMovimientoService.Application.Exceptions;
using CuentaMovimientoService.Application.Ports;
using CuentaMovimientoService.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CuentaMovimientoService.Application.Validators;

// EB-01: Retiro mayor al saldo disponible.
public class SaldoSuficienteValidator : IMovimientoValidator
{
    private readonly ILogger<SaldoSuficienteValidator>? _logger;

    public SaldoSuficienteValidator(ILogger<SaldoSuficienteValidator>? logger = null)
    {
        _logger = logger;
    }

    public Task ValidarAsync(Cuenta cuenta, decimal valor, IMovimientoRepository movimientoRepo)
    {
        if (valor >= 0)
        {
            return Task.CompletedTask;
        }

        var retiro = Math.Abs(valor);
        var saldoActual = cuenta.ObtenerSaldoActual();

        if (saldoActual < retiro)
        {
            _logger?.LogWarning("Saldo no disponible en cuenta {NumeroCuenta}: actual {SaldoActual}, requerido {Retiro}",
                cuenta.NumeroCuenta, saldoActual, retiro);
            throw new SaldoNoDisponibleException();
        }

        return Task.CompletedTask;
    }
}

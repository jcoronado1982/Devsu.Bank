using CuentaMovimientoService.Application.Exceptions;
using CuentaMovimientoService.Application.Ports;
using CuentaMovimientoService.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CuentaMovimientoService.Application.Validators;

// EB-04: Transacción sobre cuenta inactiva.
public class CuentaActivaValidator : IMovimientoValidator
{
    private readonly ILogger<CuentaActivaValidator>? _logger;

    public CuentaActivaValidator(ILogger<CuentaActivaValidator>? logger = null)
    {
        _logger = logger;
    }

    public Task ValidarAsync(Cuenta cuenta, decimal valor, IMovimientoRepository movimientoRepo)
    {
        if (!cuenta.Estado)
        {
            _logger?.LogWarning("Intento de registrar movimiento en cuenta inactiva {NumeroCuenta}", cuenta.NumeroCuenta);
            throw new CuentaInactivaException();
        }

        return Task.CompletedTask;
    }
}

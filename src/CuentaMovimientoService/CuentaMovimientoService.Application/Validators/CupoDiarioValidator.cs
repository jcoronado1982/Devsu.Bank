using CuentaMovimientoService.Application.Exceptions;
using CuentaMovimientoService.Application.Ports;
using CuentaMovimientoService.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CuentaMovimientoService.Application.Validators;

public class CupoDiarioValidator : IMovimientoValidator
{
    private const decimal LimiteDiario = 1000.00m;
    private readonly ILogger<CupoDiarioValidator>? _logger;

    public CupoDiarioValidator(ILogger<CupoDiarioValidator>? logger = null)
    {
        _logger = logger;
    }

    public async Task ValidarAsync(Cuenta cuenta, decimal valor, IMovimientoRepository movimientoRepo)
    {
        if (valor >= 0)
        {
            return;
        }

        var retiro = Math.Abs(valor);
        var totalRetiradoHoy = await movimientoRepo.ObtenerTotalRetiradoHoyAsync(cuenta.NumeroCuenta);

        if (totalRetiradoHoy + retiro > LimiteDiario)
        {
            _logger?.LogWarning("Cupo diario excedido en cuenta {NumeroCuenta}: acumulado hoy {TotalHoy}, intento {Retiro}",
                cuenta.NumeroCuenta, totalRetiradoHoy, retiro);
            throw new CupoDiarioExcedidoException();
        }
    }
}

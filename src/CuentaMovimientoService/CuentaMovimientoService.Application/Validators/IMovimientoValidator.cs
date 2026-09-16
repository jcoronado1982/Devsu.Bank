using CuentaMovimientoService.Application.Ports;
using CuentaMovimientoService.Domain.Entities;

namespace CuentaMovimientoService.Application.Validators;

public interface IMovimientoValidator
{
    Task ValidarAsync(Cuenta cuenta, decimal valor, IMovimientoRepository movimientoRepo);
}

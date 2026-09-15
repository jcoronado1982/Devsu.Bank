using CuentaMovimientoService.Application.Ports;
using CuentaMovimientoService.Domain.Entities;

namespace CuentaMovimientoService.Application.Validators;

// Strategy (PRINCIPIOS_SOLID_Y_BUENAS_PRACTICAS.md, O-Abierto/Cerrado): cada regla de
// negocio sobre un movimiento vive en su propia clase, para que agregar una regla nueva
// no obligue a modificar MovimientoService.
public interface IMovimientoValidator
{
    Task ValidarAsync(Cuenta cuenta, decimal valor, IMovimientoRepository movimientoRepo);
}

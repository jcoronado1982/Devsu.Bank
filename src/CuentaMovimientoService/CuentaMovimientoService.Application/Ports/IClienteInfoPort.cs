using CuentaMovimientoService.Domain.Entities;

namespace CuentaMovimientoService.Application.Ports;

public interface IClienteInfoPort
{
    Task<string?> ObtenerNombreClienteAsync(long clienteId);
    Task<long?> ObtenerClienteIdPorNombreAsync(string nombre);
    Task GuardarProyeccionAsync(ClienteProyeccion proyeccion);
    Task DesactivarProyeccionAsync(long clienteId);
}

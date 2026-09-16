using CuentaMovimientoService.Domain.Entities;

namespace CuentaMovimientoService.Application.Ports;

public interface IClienteInfoPort
{
    Task<string?> ObtenerNombreClienteAsync(long clienteId);
    Task<long?> ObtenerClienteIdPorNombreAsync(string nombre);
    Task GuardarProyeccionAsync(ClienteProyeccion proyeccion);
    Task DesactivarProyeccionAsync(long clienteId);

    Task<long?> ObtenerClienteIdPorIdentificacionAsync(string identificacion) => Task.FromResult<long?>(null);

    async Task<IReadOnlyList<long>> ObtenerClienteIdsPorNombreParcialAsync(string nombreParcial)
    {
        var id = await ObtenerClienteIdPorNombreAsync(nombreParcial);
        return id.HasValue ? new List<long> { id.Value } : Array.Empty<long>();
    }
}

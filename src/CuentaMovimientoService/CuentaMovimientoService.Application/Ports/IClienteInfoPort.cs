using CuentaMovimientoService.Domain.Entities;

namespace CuentaMovimientoService.Application.Ports;

public interface IClienteInfoPort
{
    Task<string?> ObtenerNombreClienteAsync(long clienteId);
    Task<long?> ObtenerClienteIdPorNombreAsync(string nombre);
    Task GuardarProyeccionAsync(ClienteProyeccion proyeccion);
    Task DesactivarProyeccionAsync(long clienteId);

    // Busca por identificación/documento (match exacto) — la vía que realmente conoce un cliente bancario.
    Task<long?> ObtenerClienteIdPorIdentificacionAsync(string identificacion) => Task.FromResult<long?>(null);

    // Busca clientes cuyo nombre contenga el texto (parcial, case-insensitive). Puede matchear 0, 1 o varios.
    async Task<IReadOnlyList<long>> ObtenerClienteIdsPorNombreParcialAsync(string nombreParcial)
    {
        var id = await ObtenerClienteIdPorNombreAsync(nombreParcial);
        return id.HasValue ? new List<long> { id.Value } : Array.Empty<long>();
    }
}

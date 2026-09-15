namespace CuentaMovimientoService.Domain.Entities;

/// <summary>
/// Proyección local de solo lectura de Clientes para mantener desacoplamiento de microservicios.
/// Recibe datos asíncronamente desde ClienteService vía RabbitMQ / MassTransit.
/// </summary>
public class ClienteProyeccion
{
    public long ClienteId { get; private set; }
    public string Nombre { get; private set; } = string.Empty;
    public string Identificacion { get; private set; } = string.Empty;
    public bool Estado { get; private set; }

    protected ClienteProyeccion() { }

    public ClienteProyeccion(long clienteId, string nombre, string identificacion, bool estado = true)
    {
        ClienteId = clienteId;
        Nombre = nombre?.Trim() ?? string.Empty;
        Identificacion = identificacion?.Trim() ?? string.Empty;
        Estado = estado;
    }

    public void Actualizar(string nombre, string identificacion, bool estado)
    {
        Nombre = nombre?.Trim() ?? string.Empty;
        Identificacion = identificacion?.Trim() ?? string.Empty;
        Estado = estado;
    }
}

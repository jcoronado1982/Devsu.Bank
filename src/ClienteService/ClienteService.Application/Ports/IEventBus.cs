namespace ClienteService.Application.Ports;

/// <summary>
/// Puerto de mensajería asíncrona hacia el resto del sistema (implementado con MassTransit +
/// RabbitMQ en Infrastructure). Es el único canal por el que ClienteService comunica cambios
/// a CuentaMovimientoService (ClienteCreadoEvent, ClienteEliminadoEvent): no existen llamadas
/// HTTP síncronas entre los dos microservicios para el camino normal de negocio.
/// </summary>
public interface IEventBus
{
    Task PublicarAsync<TEvent>(TEvent evento, CancellationToken ct = default) where TEvent : class;
}

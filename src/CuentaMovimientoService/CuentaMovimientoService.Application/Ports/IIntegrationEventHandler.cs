namespace CuentaMovimientoService.Application.Ports;

// Puerto agnóstico de broker para consumir eventos de integración: Application solo conoce
// este contrato, nunca MassTransit.IConsumer<T>. El adaptador técnico en Infrastructure/Messaging
// traduce mensajes concretos del broker hacia este puerto, simétrico a como IEventBus desacopla
// la publicación.
public interface IIntegrationEventHandler<TEvent> where TEvent : class
{
    Task HandleAsync(TEvent evento, CancellationToken ct = default);
}

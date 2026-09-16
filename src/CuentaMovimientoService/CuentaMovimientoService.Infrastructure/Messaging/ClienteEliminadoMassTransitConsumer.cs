using CuentaMovimientoService.Application.Ports;
using Devsu.Contracts.Events;
using MassTransit;

namespace CuentaMovimientoService.Infrastructure.Messaging;

// Adaptador técnico: es el único punto del sistema que conoce MassTransit.IConsumer<T>.
// Delega toda la lógica de negocio al puerto IIntegrationEventHandler<T> (Application).
public class ClienteEliminadoMassTransitConsumer : IConsumer<ClienteEliminadoEvent>
{
    private readonly IIntegrationEventHandler<ClienteEliminadoEvent> _handler;

    public ClienteEliminadoMassTransitConsumer(IIntegrationEventHandler<ClienteEliminadoEvent> handler)
    {
        _handler = handler;
    }

    public Task Consume(ConsumeContext<ClienteEliminadoEvent> context)
        => _handler.HandleAsync(context.Message, context.CancellationToken);
}

using CuentaMovimientoService.Application.Ports;
using Devsu.Contracts.Events;
using MassTransit;

namespace CuentaMovimientoService.Infrastructure.Messaging;

// Adaptador técnico: es el único punto del sistema que conoce MassTransit.IConsumer<T>.
// Delega toda la lógica de negocio al puerto IIntegrationEventHandler<T> (Application).
public class ClienteCreadoMassTransitConsumer : IConsumer<ClienteCreadoEvent>
{
    private readonly IIntegrationEventHandler<ClienteCreadoEvent> _handler;

    public ClienteCreadoMassTransitConsumer(IIntegrationEventHandler<ClienteCreadoEvent> handler)
    {
        _handler = handler;
    }

    public Task Consume(ConsumeContext<ClienteCreadoEvent> context)
        => _handler.HandleAsync(context.Message, context.CancellationToken);
}

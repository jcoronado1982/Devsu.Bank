using CuentaMovimientoService.Application.Ports;
using Devsu.Contracts.Events;
using MassTransit;

namespace CuentaMovimientoService.Infrastructure.Messaging;

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

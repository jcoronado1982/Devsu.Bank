using ClienteService.Application.Ports;
using MassTransit;

namespace ClienteService.Infrastructure.Messaging;

public class MassTransitEventBus : IEventBus
{
    private readonly IPublishEndpoint _publishEndpoint;

    public MassTransitEventBus(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public async Task PublicarAsync<TEvent>(TEvent evento, CancellationToken ct = default) where TEvent : class
    {
        await _publishEndpoint.Publish(evento, ct);
    }
}

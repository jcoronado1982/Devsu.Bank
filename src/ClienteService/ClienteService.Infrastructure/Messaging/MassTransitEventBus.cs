using ClienteService.Application.Ports;
using MassTransit;

namespace ClienteService.Infrastructure.Messaging;

/// <summary>
/// Adaptador de IEventBus sobre MassTransit + RabbitMQ. IPublishEndpoint ya gestiona la
/// serialización, el routing por tipo de mensaje y la propagación del traceparent W3C hacia
/// el broker, por lo que este adaptador es un simple passthrough.
/// </summary>
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

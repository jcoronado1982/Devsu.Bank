namespace ClienteService.Application.Ports;

public interface IEventBus
{
    Task PublicarAsync<TEvent>(TEvent evento, CancellationToken ct = default) where TEvent : class;
}

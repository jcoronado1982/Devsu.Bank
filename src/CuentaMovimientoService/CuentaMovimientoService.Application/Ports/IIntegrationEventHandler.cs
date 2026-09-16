namespace CuentaMovimientoService.Application.Ports;

public interface IIntegrationEventHandler<TEvent> where TEvent : class
{
    Task HandleAsync(TEvent evento, CancellationToken ct = default);
}

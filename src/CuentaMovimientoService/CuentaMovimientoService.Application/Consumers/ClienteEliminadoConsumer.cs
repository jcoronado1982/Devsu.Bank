using Devsu.Contracts.Events;
using CuentaMovimientoService.Application.Ports;
using Microsoft.Extensions.Logging;

namespace CuentaMovimientoService.Application.Consumers;

public class ClienteEliminadoConsumer : IIntegrationEventHandler<ClienteEliminadoEvent>
{
    private readonly IClienteInfoPort _clienteInfoPort;
    private readonly ILogger<ClienteEliminadoConsumer> _logger;

    public ClienteEliminadoConsumer(IClienteInfoPort clienteInfoPort, ILogger<ClienteEliminadoConsumer> logger)
    {
        _clienteInfoPort = clienteInfoPort;
        _logger = logger;
    }

    public async Task HandleAsync(ClienteEliminadoEvent evento, CancellationToken ct = default)
    {
        _logger.LogInformation("Consumiendo ClienteEliminadoEvent para cliente ID {ClienteId}", evento.ClienteId);

        await _clienteInfoPort.DesactivarProyeccionAsync(evento.ClienteId);

        _logger.LogInformation("Proyección de cliente {ClienteId} desactivada exitosamente", evento.ClienteId);
    }
}

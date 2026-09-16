using Devsu.Contracts.Events;
using CuentaMovimientoService.Application.Ports;
using CuentaMovimientoService.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CuentaMovimientoService.Application.Consumers;

public class ClienteCreadoConsumer : IIntegrationEventHandler<ClienteCreadoEvent>
{
    private readonly IClienteInfoPort _clienteInfoPort;
    private readonly ILogger<ClienteCreadoConsumer> _logger;

    public ClienteCreadoConsumer(IClienteInfoPort clienteInfoPort, ILogger<ClienteCreadoConsumer> logger)
    {
        _clienteInfoPort = clienteInfoPort;
        _logger = logger;
    }

    public async Task HandleAsync(ClienteCreadoEvent evento, CancellationToken ct = default)
    {
        _logger.LogInformation("Consumiendo ClienteCreadoEvent para cliente ID {ClienteId}: {Nombre}", evento.ClienteId, evento.Nombre);

        var proyeccion = new ClienteProyeccion(evento.ClienteId, evento.Nombre, evento.Identificacion, evento.Estado);
        await _clienteInfoPort.GuardarProyeccionAsync(proyeccion);

        _logger.LogInformation("Proyección de cliente {ClienteId} guardada exitosamente", evento.ClienteId);
    }
}

using CuentaMovimientoService.Application.Events;
using CuentaMovimientoService.Application.Ports;
using CuentaMovimientoService.Domain.Entities;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace CuentaMovimientoService.Application.Consumers;

public class ClienteCreadoConsumer : IConsumer<ClienteCreadoEvent>
{
    private readonly IClienteInfoPort _clienteInfoPort;
    private readonly ILogger<ClienteCreadoConsumer> _logger;

    public ClienteCreadoConsumer(IClienteInfoPort clienteInfoPort, ILogger<ClienteCreadoConsumer> logger)
    {
        _clienteInfoPort = clienteInfoPort;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ClienteCreadoEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("Consumiendo ClienteCreadoEvent para cliente ID {ClienteId}: {Nombre}", msg.ClienteId, msg.Nombre);

        var proyeccion = new ClienteProyeccion(msg.ClienteId, msg.Nombre, msg.Identificacion, msg.Estado);
        await _clienteInfoPort.GuardarProyeccionAsync(proyeccion);

        _logger.LogInformation("Proyección de cliente {ClienteId} guardada exitosamente", msg.ClienteId);
    }
}

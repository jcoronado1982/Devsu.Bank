using CuentaMovimientoService.Application.Events;
using CuentaMovimientoService.Application.Ports;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace CuentaMovimientoService.Application.Consumers;

public class ClienteEliminadoConsumer : IConsumer<ClienteEliminadoEvent>
{
    private readonly IClienteInfoPort _clienteInfoPort;
    private readonly ILogger<ClienteEliminadoConsumer> _logger;

    public ClienteEliminadoConsumer(IClienteInfoPort clienteInfoPort, ILogger<ClienteEliminadoConsumer> logger)
    {
        _clienteInfoPort = clienteInfoPort;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ClienteEliminadoEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("Consumiendo ClienteEliminadoEvent para cliente ID {ClienteId}", msg.ClienteId);

        // Soft-delete: se marca la proyeccion como inactiva en vez de borrarla,
        // para preservar la integridad referencial con cuentas/movimientos (Append-Only).
        await _clienteInfoPort.DesactivarProyeccionAsync(msg.ClienteId);

        _logger.LogInformation("Proyección de cliente {ClienteId} desactivada exitosamente", msg.ClienteId);
    }
}

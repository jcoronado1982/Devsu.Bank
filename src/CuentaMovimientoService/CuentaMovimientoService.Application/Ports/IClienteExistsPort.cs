namespace CuentaMovimientoService.Application.Ports;

// Puerto mínimo usado por CuentaService al abrir una cuenta (EB-07): solo necesita saber
// si el cliente existe y está activo, no sus datos. Ver IClienteInfoPort para lecturas más ricas
// (nombre, identificación) usadas por ReporteService. ClienteExistsPort implementa ambas interfaces.
public interface IClienteExistsPort
{
    Task<bool> ExisteClienteAsync(long clienteId);
}

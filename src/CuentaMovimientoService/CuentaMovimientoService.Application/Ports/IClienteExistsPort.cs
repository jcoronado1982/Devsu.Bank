namespace CuentaMovimientoService.Application.Ports;

public interface IClienteExistsPort
{
    Task<bool> ExisteClienteAsync(long clienteId);
}

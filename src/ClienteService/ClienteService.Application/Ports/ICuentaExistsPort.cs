namespace ClienteService.Application.Ports;

public interface ICuentaExistsPort
{
    Task<bool> ClienteTieneCuentasAsync(long clienteId);
}

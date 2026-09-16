namespace ClienteService.Application.Ports;

/// <summary>
/// Puerto síncrono hacia CuentaMovimientoService, usado exclusivamente para verificar,
/// en el instante de eliminar un cliente, si tiene cuentas asociadas. Es la única
/// excepción deliberada a la regla de "cero llamadas HTTP síncronas entre microservicios":
/// borrar un cliente es una operación rara y destructiva (no es el camino crítico de
/// transacciones), y el riesgo de dejar cuentas huérfanas no es aceptable con solo
/// consistencia eventual.
/// </summary>
public interface ICuentaExistsPort
{
    /// <summary>
    /// Devuelve true si el cliente tiene al menos una cuenta registrada en CuentaMovimientoService.
    /// Si la verificación falla (servicio caído, timeout, respuesta inválida), la implementación
    /// debe lanzar ServicioCuentasNoDisponibleException en vez de devolver false, para no permitir
    /// una eliminación insegura por datos incompletos.
    /// </summary>
    Task<bool> ClienteTieneCuentasAsync(long clienteId);
}

using System.Globalization;
using CuentaMovimientoService.Application.DTOs;
using CuentaMovimientoService.Application.Ports;
using Microsoft.Extensions.Logging;

namespace CuentaMovimientoService.Application.Services;

public class ReporteService : IReporteService
{
    private readonly ICuentaRepository _cuentaRepo;
    private readonly IMovimientoRepository _movimientoRepo;
    private readonly IClienteInfoPort _clienteInfoPort;
    private readonly ILogger<ReporteService>? _logger;

    public ReporteService(
        ICuentaRepository cuentaRepo,
        IMovimientoRepository movimientoRepo,
        IClienteInfoPort clienteInfoPort,
        ILogger<ReporteService>? logger = null)
    {
        _cuentaRepo = cuentaRepo;
        _movimientoRepo = movimientoRepo;
        _clienteInfoPort = clienteInfoPort;
        _logger = logger;
    }

    public async Task<IEnumerable<ReporteMovimientoDto>> GenerarReporteEstadoCuentaAsync(
        long clienteId, DateTime desde, DateTime hasta)
    {
        _logger?.LogInformation("Generando reporte de estado de cuenta para cliente ID {ClienteId} entre {Desde} y {Hasta}",
            clienteId, desde, hasta);

        var cuentas = await _cuentaRepo.ObtenerPorClienteIdAsync(clienteId);
        var nombreCliente = await _clienteInfoPort.ObtenerNombreClienteAsync(clienteId) ?? "Cliente";
        var resultado = new List<ReporteMovimientoDto>();

        foreach (var cuenta in cuentas)
        {
            var movimientos = await _movimientoRepo
                .ObtenerPorNumeroCuentaYFechaAsync(cuenta.NumeroCuenta, desde, hasta);

            foreach (var mov in movimientos)
            {
                resultado.Add(new ReporteMovimientoDto(
                    Fecha: mov.Fecha.ToString("d/M/yyyy"),
                    Cliente: nombreCliente,
                    NumeroCuenta: cuenta.NumeroCuenta,
                    Tipo: cuenta.TipoCuentaId == 2 ? "Corriente" : "Ahorros",
                    SaldoInicial: cuenta.SaldoInicial,
                    Estado: cuenta.Estado,
                    Movimiento: mov.Valor,
                    SaldoDisponible: mov.Saldo));
            }
        }

        // EB-09: Si no hay movimientos, retorna lista vacía
        _logger?.LogInformation("Reporte generado con {Cantidad} registros para cliente ID {ClienteId}",
            resultado.Count, clienteId);

        return resultado;
    }

    public async Task<IEnumerable<ReporteMovimientoDto>> GenerarReporteEstadoCuentaAsync(string cliente, string? fecha)
    {
        long clienteId;
        if (!long.TryParse(cliente, out clienteId))
        {
            var encontradoId = await _clienteInfoPort.ObtenerClienteIdPorNombreAsync(cliente);
            if (!encontradoId.HasValue)
            {
                // EB-09: Si no se encuentra el cliente, retorna array vacío []
                _logger?.LogInformation("Cliente '{Cliente}' no encontrado para reporte, retornando []", cliente);
                return Enumerable.Empty<ReporteMovimientoDto>();
            }
            clienteId = encontradoId.Value;
        }

        var (desde, hasta) = ParsearRangoFechas(fecha);
        return await GenerarReporteEstadoCuentaAsync(clienteId, desde, hasta);
    }

    private static (DateTime desde, DateTime hasta) ParsearRangoFechas(string? fecha)
    {
        if (string.IsNullOrWhiteSpace(fecha))
        {
            return (DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc),
                    DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc));
        }

        // Soporta formatos: "2022-01-01,2026-12-31", "2022-01-01 2026-12-31", "10/02/2022,28/02/2022"
        var separadores = new[] { ',', '|', ';' };
        var partes = fecha.Split(separadores, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (partes.Length >= 2)
        {
            var d1 = ParsearFecha(partes[0], inicioDia: true);
            var d2 = ParsearFecha(partes[1], inicioDia: false);
            return (d1, d2);
        }

        if (partes.Length == 1)
        {
            var espacioPartes = partes[0].Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (espacioPartes.Length >= 2)
            {
                var d1 = ParsearFecha(espacioPartes[0], inicioDia: true);
                var d2 = ParsearFecha(espacioPartes[1], inicioDia: false);
                return (d1, d2);
            }

            var inicio = ParsearFecha(partes[0], inicioDia: true);
            var fin = ParsearFecha(partes[0], inicioDia: false);
            return (inicio, fin);
        }

        return (DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc),
                DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc));
    }

    private static DateTime ParsearFecha(string str, bool inicioDia)
    {
        var formatos = new[] { "yyyy-MM-dd", "yyyy/MM/dd", "d/M/yyyy", "dd/MM/yyyy", "o", "s" };
        if (DateTime.TryParseExact(str, formatos, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
            || DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
        {
            var target = inicioDia ? dt.Date : dt.Date.AddDays(1).AddTicks(-1);
            return DateTime.SpecifyKind(target, DateTimeKind.Utc);
        }

        return inicioDia
            ? DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc)
            : DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);
    }
}

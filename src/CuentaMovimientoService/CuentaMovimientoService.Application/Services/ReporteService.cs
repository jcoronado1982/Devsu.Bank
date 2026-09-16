using System.Globalization;
using CuentaMovimientoService.Application.DTOs;
using CuentaMovimientoService.Application.Ports;
using Microsoft.Extensions.Logging;

namespace CuentaMovimientoService.Application.Services;

public class ReporteService : IReporteService
{
    private static readonly TimeZoneInfo ZonaHorariaReporte =
        TimeZoneInfo.CreateCustomTimeZone("Colombia", TimeSpan.FromHours(-5), "Hora Colombia", "Hora Colombia");

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
                    ClienteId: clienteId,
                    Fecha: TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(mov.Fecha, DateTimeKind.Utc), ZonaHorariaReporte)
                        .ToString("d/M/yyyy HH:mm:ss"),
                    Cliente: nombreCliente,
                    NumeroCuenta: cuenta.NumeroCuenta,
                    Tipo: cuenta.TipoCuentaId == 2 ? "Corriente" : "Ahorros",
                    SaldoInicial: cuenta.SaldoInicial,
                    SaldoActual: cuenta.ObtenerSaldoActual(),
                    Estado: cuenta.Estado,
                    TipoMovimiento: mov.TipoMovimiento,
                    Movimiento: mov.Valor,
                    SaldoDisponible: mov.Saldo));
            }
        }

        _logger?.LogInformation("Reporte generado con {Cantidad} registros para cliente ID {ClienteId}",
            resultado.Count, clienteId);

        return resultado;
    }

    public async Task<IEnumerable<ReporteMovimientoDto>> GenerarReporteEstadoCuentaAsync(string cliente, string? fecha)
    {
        var clienteIds = await ResolverClienteIdsAsync(cliente);
        if (clienteIds.Count == 0)
        {
            _logger?.LogInformation("Cliente '{Cliente}' no encontrado para reporte, retornando []", cliente);
            return Enumerable.Empty<ReporteMovimientoDto>();
        }

        var (desde, hasta) = ParsearRangoFechas(fecha);
        var resultado = new List<ReporteMovimientoDto>();
        foreach (var clienteId in clienteIds)
        {
            resultado.AddRange(await GenerarReporteEstadoCuentaAsync(clienteId, desde, hasta));
        }

        return resultado;
    }

    private const int LongitudMinimaBusquedaParcial = 3;

    private async Task<IReadOnlyList<long>> ResolverClienteIdsAsync(string cliente)
    {
        var texto = cliente.Trim();

        if (long.TryParse(texto, out var clienteId))
        {
            var existe = await _clienteInfoPort.ObtenerNombreClienteAsync(clienteId);
            if (existe is not null)
            {
                return new List<long> { clienteId };
            }
        }

        var idPorIdentificacion = await _clienteInfoPort.ObtenerClienteIdPorIdentificacionAsync(texto);
        if (idPorIdentificacion.HasValue)
        {
            return new List<long> { idPorIdentificacion.Value };
        }

        if (texto.Length < LongitudMinimaBusquedaParcial)
        {
            return Array.Empty<long>();
        }

        return await _clienteInfoPort.ObtenerClienteIdsPorNombreParcialAsync(texto);
    }

    private static (DateTime desde, DateTime hasta) ParsearRangoFechas(string? fecha)
    {
        if (string.IsNullOrWhiteSpace(fecha))
        {
            return (DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc),
                    DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc));
        }

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

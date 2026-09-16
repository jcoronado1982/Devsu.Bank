using System.Text.Json.Serialization;

namespace CuentaMovimientoService.Application.DTOs;

public class ReporteClienteResponseDto
{
    [JsonPropertyName("clienteId")]
    public long ClienteId { get; set; }

    [JsonPropertyName("cliente")]
    public string Cliente { get; set; } = string.Empty;

    [JsonPropertyName("cuentas")]
    public List<ReporteCuentaResponseDto> Cuentas { get; set; } = new();

    public static List<ReporteClienteResponseDto> AgruparDesdeMovimientos(IEnumerable<ReporteMovimientoDto> items)
    {
        return items
            .GroupBy(i => i.ClienteId)
            .Select(clienteGrupo => new ReporteClienteResponseDto
            {
                ClienteId = clienteGrupo.Key,
                Cliente = clienteGrupo.First().Cliente,
                Cuentas = clienteGrupo
                    .GroupBy(i => i.NumeroCuenta)
                    .Select(cuentaGrupo => new ReporteCuentaResponseDto
                    {
                        NumeroCuenta = cuentaGrupo.Key,
                        Tipo = cuentaGrupo.First().Tipo,
                        SaldoInicial = cuentaGrupo.First().SaldoInicial,
                        SaldoActual = cuentaGrupo.First().SaldoActual,
                        Estado = cuentaGrupo.First().Estado,
                        Movimientos = cuentaGrupo
                            .Select(m => new ReporteMovimientoResponseDto
                            {
                                Fecha = m.Fecha,
                                TipoMovimiento = m.TipoMovimiento,
                                Movimiento = m.Movimiento,
                                SaldoDisponible = m.SaldoDisponible
                            })
                            .ToList()
                    })
                    .ToList()
            })
            .ToList();
    }
}

public class ReporteCuentaResponseDto
{
    [JsonPropertyName("numeroCuenta")]
    public string NumeroCuenta { get; set; } = string.Empty;

    [JsonPropertyName("tipo")]
    public string Tipo { get; set; } = string.Empty;

    [JsonPropertyName("saldoInicial")]
    public decimal SaldoInicial { get; set; }

    [JsonPropertyName("saldoActual")]
    public decimal SaldoActual { get; set; }

    [JsonPropertyName("estado")]
    public bool Estado { get; set; }

    [JsonPropertyName("movimientos")]
    public List<ReporteMovimientoResponseDto> Movimientos { get; set; } = new();
}

public class ReporteMovimientoResponseDto
{
    [JsonPropertyName("fecha")]
    public string Fecha { get; set; } = string.Empty;

    [JsonPropertyName("tipoMovimiento")]
    public string TipoMovimiento { get; set; } = string.Empty;

    [JsonPropertyName("movimiento")]
    public decimal Movimiento { get; set; }

    [JsonPropertyName("saldoDisponible")]
    public decimal SaldoDisponible { get; set; }
}

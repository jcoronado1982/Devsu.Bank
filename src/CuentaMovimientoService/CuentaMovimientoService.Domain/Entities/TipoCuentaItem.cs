namespace CuentaMovimientoService.Domain.Entities;

/// <summary>
/// Catálogo normalizado de Tipos de Cuenta Bancaria (3NF / ISO 20022).
/// </summary>
public class TipoCuentaItem
{
    public short TipoCuentaId { get; private set; }
    public string Codigo { get; private set; } = string.Empty;
    public string Nombre { get; private set; } = string.Empty;

    protected TipoCuentaItem() { }

    public TipoCuentaItem(short tipoCuentaId, string codigo, string nombre)
    {
        if (string.IsNullOrWhiteSpace(codigo))
            throw new ArgumentException("El código es obligatorio.", nameof(codigo));

        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre es obligatorio.", nameof(nombre));

        TipoCuentaId = tipoCuentaId;
        Codigo = codigo.Trim().ToUpperInvariant();
        Nombre = nombre.Trim();
    }
}

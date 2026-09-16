namespace CuentaMovimientoService.Domain.Entities;

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

namespace ClienteService.Domain.Entities;

/// <summary>
/// Entidad de Cliente bancario que especializa a Persona (Herencia TPT).
/// </summary>
public class Cliente : Persona
{
    public long ClienteId => Id;
    public string Contrasena { get; private set; } = string.Empty;
    public bool Estado { get; private set; }

    protected Cliente() : base() { }

    public Cliente(
        string nombre,
        string genero,
        int edad,
        string identificacion,
        string direccion,
        string telefono,
        string contrasena,
        bool estado = true)
        : base(nombre, genero, edad, identificacion, direccion, telefono)
    {
        CambiarContrasena(contrasena);
        Estado = estado;
    }

    public void CambiarContrasena(string contrasena)
    {
        if (string.IsNullOrWhiteSpace(contrasena))
            throw new ArgumentException("La contraseña es obligatoria.", nameof(contrasena));

        Contrasena = contrasena;
    }

    public void Activar() => Estado = true;
    public void Inactivar() => Estado = false;
}

namespace ClienteService.Domain.Entities;

/// <summary>
/// Entidad de Cliente bancario que especializa a Persona mediante herencia Table-per-Type (TPT):
/// los campos comunes de persona natural (nombre, identificación, etc.) viven en la tabla
/// "personas" y los campos exclusivos del cliente (contraseña, estado) viven en la tabla
/// "clientes", que comparte la misma clave primaria. Esto permite reutilizar Persona como
/// entidad base sin duplicar columnas y sin acoplar el dominio a un motor de persistencia.
/// </summary>
public class Cliente : Persona
{
    public long ClienteId => Id;

    // Se persiste siempre como hash BCrypt (nunca texto plano): la capa Application
    // (ClienteService) hashea la contraseña antes de invocar el constructor o CambiarContrasena.
    // Esta entidad no conoce el algoritmo de hasheo; solo garantiza que el valor no sea vacío.
    public string Contrasena { get; private set; } = string.Empty;
    public bool Estado { get; private set; }

    /// <summary>
    /// Constructor protegido reservado para EF Core (materialización desde la base de datos).
    /// </summary>
    protected Cliente() : base() { }

    /// <summary>
    /// Crea un nuevo Cliente activo por defecto. La contraseña recibida aquí debe llegar
    /// ya hasheada por el llamador; esta entidad no aplica ninguna transformación adicional.
    /// </summary>
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

    /// <summary>
    /// Única vía para mutar la contraseña del cliente (el setter de <see cref="Contrasena"/>
    /// es privado). Solo valida que no venga vacía; el hasheo con BCrypt es responsabilidad
    /// de la capa Application antes de llamar a este método.
    /// </summary>
    public void CambiarContrasena(string contrasena)
    {
        if (string.IsNullOrWhiteSpace(contrasena))
            throw new ArgumentException("La contraseña es obligatoria.", nameof(contrasena));

        Contrasena = contrasena;
    }

    /// <summary>Marca al cliente como activo (Estado = true).</summary>
    public void Activar() => Estado = true;

    /// <summary>Marca al cliente como inactivo (Estado = false), sin eliminar sus datos.</summary>
    public void Inactivar() => Estado = false;
}

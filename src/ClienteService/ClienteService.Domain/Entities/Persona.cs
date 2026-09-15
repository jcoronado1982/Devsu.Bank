namespace ClienteService.Domain.Entities;

/// <summary>
/// Entidad base que representa los datos de una persona natural.
/// </summary>
public class Persona
{
    public long PersonaId { get; protected set; }
    public long Id => PersonaId;
    public string Nombre { get; protected set; } = string.Empty;
    public string Genero { get; protected set; } = string.Empty;
    public int Edad { get; protected set; }
    public string Identificacion { get; protected set; } = string.Empty;
    public string Direccion { get; protected set; } = string.Empty;
    public string Telefono { get; protected set; } = string.Empty;

    protected Persona() { }

    public Persona(string nombre, string genero, int edad, string identificacion, string direccion, string telefono)
    {
        if (string.IsNullOrWhiteSpace(identificacion) || identificacion.Trim().Length < 3)
            throw new ArgumentException("La identificación es obligatoria y debe tener al menos 3 caracteres.", nameof(identificacion));

        // La identificación es inmutable (DICCIONARIO_DE_DATOS_Y_TIPOS.md): solo se fija al crear la persona.
        Identificacion = identificacion.Trim();

        ActualizarDatosPersona(nombre, genero, edad, direccion, telefono);
    }

    public void ActualizarDatosPersona(string nombre, string genero, int edad, string direccion, string telefono)
    {
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length < 2)
            throw new ArgumentException("El nombre es obligatorio y debe tener al menos 2 caracteres.", nameof(nombre));

        if (edad < 0 || edad > 120)
            throw new ArgumentOutOfRangeException(nameof(edad), "La edad debe estar comprendida entre 0 y 120 años.");

        Nombre = nombre.Trim();
        Genero = genero?.Trim() ?? string.Empty;
        Edad = edad;
        Direccion = direccion?.Trim() ?? string.Empty;
        Telefono = telefono?.Trim() ?? string.Empty;
    }
}

using System.Linq;
using System.Text.RegularExpressions;

namespace ClienteService.Domain.Entities;

public class Persona
{
    private static readonly Regex SoloLetrasYEspacios = new(@"^[\p{L}\s]+$", RegexOptions.Compiled);
    private static readonly Regex Alfanumerico = new(@"^[A-Za-z0-9]+$", RegexOptions.Compiled);
    private static readonly Regex TelefonoValido = new(@"^\+?[0-9]{7,20}$", RegexOptions.Compiled);
    private static readonly string[] GenerosPermitidos = { "Masculino", "Femenino" };

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

        var identificacionTrim = identificacion.Trim();
        if (identificacionTrim.Length < 5 || identificacionTrim.Length > 20 || !Alfanumerico.IsMatch(identificacionTrim))
            throw new ArgumentException("La identificación debe tener entre 5 y 20 caracteres alfanuméricos.", nameof(identificacion));

        Identificacion = identificacionTrim.ToUpperInvariant();

        ActualizarDatosPersona(nombre, genero, edad, direccion, telefono);
    }

    public void ActualizarDatosPersona(string nombre, string genero, int edad, string direccion, string telefono)
    {
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length < 2)
            throw new ArgumentException("El nombre es obligatorio y debe tener al menos 2 caracteres.", nameof(nombre));

        var nombreTrim = nombre.Trim();
        if (!SoloLetrasYEspacios.IsMatch(nombreTrim))
            throw new ArgumentException("El nombre solo puede contener caracteres alfabéticos y espacios.", nameof(nombre));

        if (edad < 18 || edad > 120)
            throw new ArgumentOutOfRangeException(nameof(edad), "La edad debe estar comprendida entre 18 y 120 años.");

        var generoNormalizado = string.IsNullOrWhiteSpace(genero)
            ? null
            : GenerosPermitidos.FirstOrDefault(g => g.Equals(genero.Trim(), StringComparison.OrdinalIgnoreCase));

        if (generoNormalizado is null)
            throw new ArgumentException("El género debe ser uno de los siguientes valores: Masculino, Femenino.", nameof(genero));

        if (string.IsNullOrWhiteSpace(direccion))
            throw new ArgumentException("La dirección es obligatoria.", nameof(direccion));

        if (string.IsNullOrWhiteSpace(telefono) || !TelefonoValido.IsMatch(telefono.Trim()))
            throw new ArgumentException("El teléfono es obligatorio y debe tener entre 7 y 20 caracteres numéricos (opcionalmente con prefijo +).", nameof(telefono));

        Nombre = nombreTrim;
        Genero = generoNormalizado;
        Edad = edad;
        Direccion = direccion.Trim();
        Telefono = telefono.Trim();
    }
}

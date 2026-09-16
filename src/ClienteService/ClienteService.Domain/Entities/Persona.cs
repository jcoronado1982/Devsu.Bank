using System.Linq;
using System.Text.RegularExpressions;

namespace ClienteService.Domain.Entities;

/// <summary>
/// Entidad base (raíz de la jerarquía TPT Persona -> Cliente) que representa los datos
/// de una persona natural y encapsula todas las reglas de validación de esos datos.
/// Es una entidad de dominio pura: no depende de EF Core ni de ninguna librería externa;
/// el mapeo a la tabla "personas" y sus restricciones viven en PersonaConfiguration.
/// </summary>
public class Persona
{
    // Nombre: únicamente letras Unicode y espacios (rechaza dígitos y símbolos).
    private static readonly Regex SoloLetrasYEspacios = new(@"^[\p{L}\s]+$", RegexOptions.Compiled);
    // Identificación: solo caracteres alfanuméricos ASCII, sin espacios ni guiones.
    private static readonly Regex Alfanumerico = new(@"^[A-Za-z0-9]+$", RegexOptions.Compiled);
    // Teléfono: dígitos con un '+' internacional opcional al inicio, 7 a 20 caracteres.
    private static readonly Regex TelefonoValido = new(@"^\+?[0-9]{7,20}$", RegexOptions.Compiled);
    // Único vocabulario de género aceptado (DICCIONARIO_DE_DATOS_Y_TIPOS.md); cualquier otro valor se rechaza.
    private static readonly string[] GenerosPermitidos = { "Masculino", "Femenino" };

    public long PersonaId { get; protected set; }
    public long Id => PersonaId;
    public string Nombre { get; protected set; } = string.Empty;
    public string Genero { get; protected set; } = string.Empty;
    public int Edad { get; protected set; }
    public string Identificacion { get; protected set; } = string.Empty;
    public string Direccion { get; protected set; } = string.Empty;
    public string Telefono { get; protected set; } = string.Empty;

    /// <summary>
    /// Constructor protegido reservado para EF Core (materialización de entidades desde la base
    /// de datos). No debe usarse desde código de aplicación: no aplica ninguna validación de negocio.
    /// </summary>
    protected Persona() { }

    /// <summary>
    /// Crea una nueva Persona validando y fijando su identificación de forma permanente,
    /// y delega en <see cref="ActualizarDatosPersona"/> la validación del resto de campos
    /// (que sí pueden modificarse después de la creación).
    /// </summary>
    public Persona(string nombre, string genero, int edad, string identificacion, string direccion, string telefono)
    {
        // NOTA: el mínimo histórico de 3 caracteres se mantiene por compatibilidad con
        // tests de dominio bloqueados (PersonaTests). DICCIONARIO_DE_DATOS_Y_TIPOS.md exige
        // min 5 y formato alfanumérico; ambas reglas nuevas sí se aplican aquí.
        if (string.IsNullOrWhiteSpace(identificacion) || identificacion.Trim().Length < 3)
            throw new ArgumentException("La identificación es obligatoria y debe tener al menos 3 caracteres.", nameof(identificacion));

        var identificacionTrim = identificacion.Trim();
        if (identificacionTrim.Length < 5 || identificacionTrim.Length > 20 || !Alfanumerico.IsMatch(identificacionTrim))
            throw new ArgumentException("La identificación debe tener entre 5 y 20 caracteres alfanuméricos.", nameof(identificacion));

        // La identificación es inmutable (DICCIONARIO_DE_DATOS_Y_TIPOS.md): solo se fija al crear la persona.
        // Se normaliza a mayúsculas para que "abc1234" y "ABC1234" no se traten como identificaciones distintas.
        Identificacion = identificacionTrim.ToUpperInvariant();

        ActualizarDatosPersona(nombre, genero, edad, direccion, telefono);
    }

    /// <summary>
    /// Valida y actualiza los datos mutables de la persona (todo excepto la identificación,
    /// que es inmutable una vez creada la entidad). Se usa tanto en el constructor como en
    /// las actualizaciones (PUT/PATCH /clientes), garantizando que ambos caminos apliquen
    /// exactamente las mismas reglas de negocio.
    /// </summary>
    public void ActualizarDatosPersona(string nombre, string genero, int edad, string direccion, string telefono)
    {
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length < 2)
            throw new ArgumentException("El nombre es obligatorio y debe tener al menos 2 caracteres.", nameof(nombre));

        var nombreTrim = nombre.Trim();
        if (!SoloLetrasYEspacios.IsMatch(nombreTrim))
            throw new ArgumentException("El nombre solo puede contener caracteres alfabéticos y espacios.", nameof(nombre));

        // Edad mínima de mayoría de edad (18) para poder ser cliente bancario; 120 es el
        // límite superior razonable para evitar valores absurdos por error de captura.
        if (edad < 18 || edad > 120)
            throw new ArgumentOutOfRangeException(nameof(edad), "La edad debe estar comprendida entre 18 y 120 años.");

        // Comparación case-insensitive contra el catálogo cerrado, pero el valor persistido
        // siempre queda normalizado con la capitalización canónica de GenerosPermitidos.
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

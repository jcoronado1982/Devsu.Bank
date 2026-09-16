using ClienteService.Application.Ports;

namespace ClienteService.Infrastructure.Security;

/// <summary>
/// Implementación de IPasswordHasher con BCrypt. WorkFactor 12 es el estándar bancario de seguridad:
/// suficientemente costoso para dificultar ataques de fuerza bruta/rainbow table,
/// sin degradar de forma inaceptable la latencia de creación/login de clientes.
/// Se registra como Singleton en Program.cs porque el algoritmo es puro y sin estado mutable.
/// </summary>
public class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string HashPassword(string plainPassword)
    {
        if (string.IsNullOrWhiteSpace(plainPassword))
            throw new ArgumentException("La contraseña a hashear no puede estar vacía.", nameof(plainPassword));

        return BCrypt.Net.BCrypt.HashPassword(plainPassword, WorkFactor);
    }

    public bool VerifyPassword(string plainPassword, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(plainPassword) || string.IsNullOrWhiteSpace(passwordHash))
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(plainPassword, passwordHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // Un hash corrupto o no-BCrypt debe fallar de forma segura (fail-closed),
            // nunca propagar una excepción no controlada hasta el flujo de login.
            return false;
        }
    }
}

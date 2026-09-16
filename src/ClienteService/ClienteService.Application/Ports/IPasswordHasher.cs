namespace ClienteService.Application.Ports;

/// <summary>
/// Puerto de hasheo de contraseñas (Strategy/Adapter). Implementado en Infrastructure con
/// BCrypt WorkFactor 12 (BcryptPasswordHasher), registrado como Singleton porque el algoritmo
/// es puro y sin estado. Application solo conoce este contrato, nunca el algoritmo concreto.
/// </summary>
public interface IPasswordHasher
{
    string HashPassword(string plainPassword);
    bool VerifyPassword(string plainPassword, string passwordHash);
}

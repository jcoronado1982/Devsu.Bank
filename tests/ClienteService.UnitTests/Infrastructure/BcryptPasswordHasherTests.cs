using System;
using ClienteService.Infrastructure.Security;
using FluentAssertions;
using Xunit;

namespace ClienteService.UnitTests.Infrastructure;

/// <summary>
/// Verifica el adaptador BCrypt real: WorkFactor 12,
/// hash unidireccional y que la contraseña jamás sea recuperable.
/// </summary>
public class BcryptPasswordHasherTests
{
    private readonly BcryptPasswordHasher _hasher = new();

    [Fact]
    public void HashPassword_ConWorkFactor12_DebeGenerarHashConPrefijoBCryptEsperado()
    {
        // Act
        var hash = _hasher.HashPassword("miContrasenaSegura123");

        // Assert — BCrypt.Net-Next codifica el work factor en el propio hash: $2<variante>$12$...
        hash.Should().MatchRegex(@"^\$2[aby]\$12\$.+");
        hash.Should().NotBe("miContrasenaSegura123");
    }

    [Fact]
    public void HashPassword_MismaContrasenaDosVeces_DebeGenerarHashesDistintos()
    {
        // Act — BCrypt usa un salt aleatorio por invocación; nunca debe ser determinístico
        var hash1 = _hasher.HashPassword("1234");
        var hash2 = _hasher.HashPassword("1234");

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void VerifyPassword_ConLaContrasenaCorrecta_DebeRetornarTrue()
    {
        // Arrange
        var hash = _hasher.HashPassword("1234");

        // Act & Assert — round-trip real (no el eco textual del FakePasswordHasher)
        _hasher.VerifyPassword("1234", hash).Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_ConContrasenaIncorrecta_DebeRetornarFalse()
    {
        // Arrange
        var hash = _hasher.HashPassword("1234");

        // Act & Assert
        _hasher.VerifyPassword("otra-contrasena", hash).Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_ConHashCorruptoOInvalido_DebeRetornarFalse_NoLanzarExcepcion()
    {
        // Act & Assert — un hash malformado no debe tumbar el proceso de login
        _hasher.VerifyPassword("1234", "esto-no-es-un-hash-bcrypt-valido").Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void HashPassword_ConContrasenaVacia_DebeLanzarArgumentException(string? contrasenaInvalida)
    {
        // Act
        Action act = () => _hasher.HashPassword(contrasenaInvalida!);

        // Assert — Fail-Fast: nunca se debe intentar hashear un valor vacío
        act.Should().Throw<ArgumentException>();
    }
}

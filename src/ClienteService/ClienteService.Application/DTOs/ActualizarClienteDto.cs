namespace ClienteService.Application.DTOs;

/// <summary>
/// DTO de entrada para PUT/PATCH /clientes/{id}. Deliberadamente no incluye Identificacion:
/// ese campo es inmutable una vez creado el cliente (ver Persona.Identificacion) y aceptarlo
/// aquí sería una vía de overposting para intentar cambiarlo. Contrasena es opcional en la
/// práctica: ClienteService solo la re-hashea y aplica si viene no vacía; si llega vacía o en
/// blanco, se conserva la contraseña actual sin cambios.
/// </summary>
public record ActualizarClienteDto(
    string Nombre,
    string Genero,
    int Edad,
    string Direccion,
    string Telefono,
    string Contrasena,
    bool Estado
);

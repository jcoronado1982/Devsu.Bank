namespace Devsu.Contracts.Events;

/// <summary>
/// Contrato de evento compartido entre ClienteService (publicador) y CuentaMovimientoService
/// (consumidor). MassTransit identifica el tipo de mensaje por su nombre completo (namespace
/// incluido); si cada microservicio definiera su propia copia de este record en su propio
/// namespace, el publicador y el consumidor terminarían usando exchanges distintos y el
/// evento nunca llegaría a destino. Por eso este contrato vive en un proyecto neutral,
/// referenciado únicamente por la capa Application de cada microservicio (nunca por Domain).
/// </summary>
public record ClienteCreadoEvent(
    long ClienteId,
    string Nombre,
    string Identificacion,
    string Direccion,
    string Telefono,
    bool Estado
);

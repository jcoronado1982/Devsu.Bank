# =========================================================================
# Multi-stage Dockerfile para CuentaMovimientoService (.NET 9 / Linux Alpine)
# =========================================================================

FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /app

# Copiar código fuente completo para restaurar y compilar de forma consistente
COPY src/CuentaMovimientoService/ src/CuentaMovimientoService/

RUN dotnet publish src/CuentaMovimientoService/CuentaMovimientoService.Api/CuentaMovimientoService.Api.csproj \
    -c Release \
    -o /app/publish

# Runtime Final Ultra Ligero en Alpine
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app

USER $APP_UID

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_EnableDiagnostics=0

EXPOSE 8080

ENTRYPOINT ["dotnet", "CuentaMovimientoService.Api.dll"]

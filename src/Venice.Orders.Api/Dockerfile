# ========== build ==========
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia apenas csproj para otimizar cache do restore
COPY src/Venice.Orders.Api/Venice.Orders.Api.csproj src/Venice.Orders.Api/
COPY src/Venice.Orders.Application/Venice.Orders.Application.csproj src/Venice.Orders.Application/
COPY src/Venice.Orders.Domain/Venice.Orders.Domain.csproj src/Venice.Orders.Domain/
COPY src/Venice.Orders.Infrastructure/Venice.Orders.Infrastructure.csproj src/Venice.Orders.Infrastructure/

RUN dotnet restore src/Venice.Orders.Api/Venice.Orders.Api.csproj

# Copia o restante do código
COPY . .

# Publica em modo Release, self-contained = false (usa runtime base)
RUN dotnet publish src/Venice.Orders.Api/Venice.Orders.Api.csproj -c Release -o /app/publish --no-restore

# ========== runtime ==========
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Porta padrão exposta pelo container
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# (Opcional) timezone para BRT — remova se não precisar
# RUN ln -snf /usr/share/zoneinfo/America/Sao_Paulo /etc/localtime && echo America/Sao_Paulo > /etc/timezone

COPY --from=build /app/publish .

# (Opcional) healthcheck simples HTTP
# HEALTHCHECK --interval=30s --timeout=3s --start-period=20s --retries=3 \
#   CMD wget -qO- http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Venice.Orders.Api.dll"]

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/WordDuel.Domain/WordDuel.Domain.csproj src/WordDuel.Domain/
COPY src/WordDuel.Infrastructure/WordDuel.Infrastructure.csproj src/WordDuel.Infrastructure/
COPY src/WordDuel.Api/WordDuel.Api.csproj src/WordDuel.Api/
RUN dotnet restore src/WordDuel.Api/WordDuel.Api.csproj

COPY src/ src/
RUN dotnet publish src/WordDuel.Api/WordDuel.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && useradd --create-home --shell /bin/false appuser

COPY --from=build /app .
RUN chown -R appuser:appuser /app
USER appuser

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=10s --timeout=3s --start-period=20s --retries=5 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "WordDuel.Api.dll"]

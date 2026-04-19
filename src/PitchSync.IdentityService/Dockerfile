FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files first for layer caching
COPY src/PitchSync.Shared/PitchSync.Shared.csproj src/PitchSync.Shared/
COPY src/PitchSync.IdentityService/PitchSync.IdentityService.csproj src/PitchSync.IdentityService/

RUN dotnet restore src/PitchSync.IdentityService/PitchSync.IdentityService.csproj

# Copy all source
COPY src/PitchSync.Shared/ src/PitchSync.Shared/
COPY src/PitchSync.IdentityService/ src/PitchSync.IdentityService/

RUN dotnet publish src/PitchSync.IdentityService/PitchSync.IdentityService.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser

COPY --from=build /app/publish .

RUN chown -R appuser:appgroup /app
USER appuser

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/healthz || exit 1

ENTRYPOINT ["dotnet", "PitchSync.IdentityService.dll"]

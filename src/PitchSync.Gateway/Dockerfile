FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files first for layer caching
COPY src/PitchSync.Shared/PitchSync.Shared.csproj src/PitchSync.Shared/
COPY src/PitchSync.Gateway/PitchSync.Gateway.csproj src/PitchSync.Gateway/

RUN dotnet restore src/PitchSync.Gateway/PitchSync.Gateway.csproj

# Copy all source
COPY src/PitchSync.Shared/ src/PitchSync.Shared/
COPY src/PitchSync.Gateway/ src/PitchSync.Gateway/

RUN dotnet publish src/PitchSync.Gateway/PitchSync.Gateway.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app

RUN addgroup -S appgroup && adduser -S appuser -G appgroup

COPY --from=build /app/publish .

RUN chown -R appuser:appgroup /app
USER appuser

EXPOSE 5000

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD wget -qO- http://localhost:5000/healthz || exit 1

ENTRYPOINT ["dotnet", "PitchSync.Gateway.dll"]

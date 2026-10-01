# ──────────────────────────────────────────────────────────────────────────────
# Stage 1: Build & Publish
#   Copies only project files first to maximize Docker layer caching.
#   Restores NuGet dependencies for AegisSchedule.Api.
#   Publishes a Release binary with no self-contained host wrapper.
# ──────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Layer cache: restore step (re-runs only when .csproj files change)
COPY AegisSchedule.sln .
COPY src/AegisSchedule.Api/AegisSchedule.Api.csproj             src/AegisSchedule.Api/
COPY src/AegisSchedule.Web/AegisSchedule.Web.csproj             src/AegisSchedule.Web/
COPY tests/AegisSchedule.Tests/AegisSchedule.Tests.csproj       tests/AegisSchedule.Tests/

RUN dotnet restore src/AegisSchedule.Api/AegisSchedule.Api.csproj

# Copy remaining source and publish
COPY src/AegisSchedule.Api/  src/AegisSchedule.Api/

WORKDIR /source/src/AegisSchedule.Api
RUN dotnet publish AegisSchedule.Api.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# ──────────────────────────────────────────────────────────────────────────────
# Stage 2: Runtime image
#   Uses the trimmed ASP.NET Core runtime (no SDK).
#   Drops root privileges by switching to the built-in 'app' user (UID 1654).
#   ASPNETCORE_HTTP_PORTS=8080 is the .NET 8+ convention; no EXPOSE + URLS needed.
# ──────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080

ENV ASPNETCORE_HTTP_PORTS=8080

USER app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "AegisSchedule.Api.dll"]

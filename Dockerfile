# --- Build stage: restore, publish a self-contained framework-dependent app ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder

WORKDIR /src

# Restore first (cached unless the project files change).
COPY global.json ./
COPY src/DyDashboard.Api/DyDashboard.Api.csproj src/DyDashboard.Api/
RUN dotnet restore src/DyDashboard.Api/DyDashboard.Api.csproj

# Compile and publish.
COPY src/ src/
RUN dotnet publish src/DyDashboard.Api/DyDashboard.Api.csproj -c Release -o /app/publish --no-restore

# --- Runtime stage: slim ASP.NET image with just the published output ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

ENV ASPNETCORE_ENVIRONMENT=Production
# The app binds Kestrel to $PORT (Render injects its own; 3001 by default).
ENV PORT=3001
WORKDIR /app

COPY --from=builder /app/publish ./

EXPOSE 3001
ENTRYPOINT ["dotnet", "DyDashboard.Api.dll"]

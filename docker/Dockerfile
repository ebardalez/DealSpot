# ─── Build Stage ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first for layer caching
COPY DealSpot.sln .
COPY src/DealSpot.Domain/DealSpot.Domain.csproj src/DealSpot.Domain/
COPY src/DealSpot.Application/DealSpot.Application.csproj src/DealSpot.Application/
COPY src/DealSpot.Infrastructure/DealSpot.Infrastructure.csproj src/DealSpot.Infrastructure/
COPY src/DealSpot.Web/DealSpot.Web.csproj src/DealSpot.Web/

RUN dotnet restore

COPY . .
RUN dotnet publish src/DealSpot.Web/DealSpot.Web.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ─── Runtime Stage ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Run as non-root user
RUN addgroup --system --gid 1001 dotnet \
 && adduser --system --uid 1001 --gid 1001 --no-create-home dotnet

COPY --from=build /app/publish .
RUN chown -R dotnet:dotnet /app
USER dotnet

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "DealSpot.Web.dll"]

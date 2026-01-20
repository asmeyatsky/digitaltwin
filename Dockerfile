# Multi-stage build for production optimization
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
ENV ASPNETCORE_URLS=http://+:80;https://+:443

# Install required system packages
RUN apt-get update && apt-get install -y \
    curl \
    wget \
    unzip \
    sqlite3 \
    && rm -rf /var/lib/apt/lists/*

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/API/DigitalTwin.API/DigitalTwin.API.csproj", "src/API/DigitalTwin.API/"]
COPY ["src/Core/DigitalTwin.Core/DigitalTwin.Core.csproj", "src/Core/DigitalTwin.Core/"]
COPY ["src/Infrastructure/DigitalTwin.Infrastructure/DigitalTwin.Infrastructure.csproj", "src/Infrastructure/DigitalTwin.Infrastructure/"]
COPY ["src/Presentation/DigitalTwin.Presentation/DigitalTwin.Presentation.csproj", "src/Presentation/DigitalTwin.Presentation/"]
COPY ["src/Infrastructure/DigitalTwin.Infrastructure.Tests/DigitalTwin.Infrastructure.Tests.csproj", "src/Infrastructure/DigitalTwin.Infrastructure.Tests/"]

# Restore NuGet packages
RUN dotnet restore "src/API/DigitalTwin.API/DigitalTwin.API.csproj"
RUN dotnet restore "src/Core/DigitalTwin.Core/DigitalTwin.Core.csproj"
RUN dotnet restore "src/Infrastructure/DigitalTwin.Infrastructure/DigitalTwin.Infrastructure.csproj"
RUN dotnet restore "src/Presentation/DigitalTwin.Presentation/DigitalTwin.Presentation.csproj"

# Copy all source files
COPY . .

# Build and publish
WORKDIR "/src/src/API/DigitalTwin.API"
RUN dotnet publish "DigitalTwin.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Build Unity application
WORKDIR "/src/Assets/_Project"
RUN echo "Unity build would be performed here in Unity Editor or via Unity Cloud Build"
RUN echo "For now, we'll create a placeholder for Unity WebGL build"

# Create Unity WebGL build placeholder
RUN mkdir -p /app/publish/wwwroot/unity
RUN echo "Digital Twin Unity Application" > /app/publish/wwwroot/unity/index.html

# Final stage
FROM base AS final
WORKDIR /app

# Copy published API
COPY --from=build /app/publish .

# Create non-root user for security
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

# Health check
HEALTHCHECK --interval=30s --timeout=3s \
    CMD curl -f http://localhost/health || exit 1

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=production
ENV ASPNETCORE_URLS=http://+:80;https://+:443

# Start the application
ENTRYPOINT ["dotnet", "DigitalTwin.API.dll"]
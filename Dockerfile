# Use the official .NET 8.0 runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Use the .NET 8.0 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution file
COPY TaskListMcp.sln .

# Copy project files
COPY src/TaskListMcp.Models/TaskListMcp.Models.csproj src/TaskListMcp.Models/
COPY src/TaskListMcp.Data/TaskListMcp.Data.csproj src/TaskListMcp.Data/
COPY src/TaskListMcp.Core/TaskListMcp.Core.csproj src/TaskListMcp.Core/
COPY src/TaskListMcp.Server/TaskListMcp.Server.csproj src/TaskListMcp.Server/

# Restore dependencies
RUN dotnet nuget locals all --clear
RUN dotnet restore --verbosity normal

# Copy source code
COPY src/ src/

# Build the application
RUN dotnet build -c Release

# Publish the application
FROM build AS publish
RUN dotnet publish src/TaskListMcp.Server/TaskListMcp.Server.csproj -c Release -o /app/publish --no-restore

# Final stage
FROM base AS final
WORKDIR /app

# Install sqlite3 and curl for database operations and health checks
RUN apt-get update && apt-get install -y sqlite3 curl && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=publish /app/publish .

# Create directory for database
RUN mkdir -p /app/data

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV TASKLIST_DB_PATH=/app/data/tasks.db
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Create non-root user for security
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "TaskListMcp.Server.dll"]

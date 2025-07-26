@echo off
REM Task List MCP Server Deployment Script for Windows
REM Usage: deploy.bat [environment]

setlocal enabledelayedexpansion

set ENVIRONMENT=%1
if "%ENVIRONMENT%"=="" set ENVIRONMENT=development

echo 🚀 Deploying Task List MCP Server (Environment: %ENVIRONMENT%)

REM Load environment-specific variables
if exist ".env.%ENVIRONMENT%" (
    echo 📝 Loading environment configuration from .env.%ENVIRONMENT%
    for /f "delims=" %%a in (.env.%ENVIRONMENT%) do (
        set "%%a"
    )
) else (
    echo ⚠️  No environment file found for %ENVIRONMENT%, using defaults
)

REM Build and deploy based on environment
if "%ENVIRONMENT%"=="production" (
    echo 🏭 Production deployment
    docker-compose --profile production up -d --build
) else if "%ENVIRONMENT%"=="staging" (
    echo 🧪 Staging deployment
    docker-compose up -d --build
) else (
    echo 🛠️  Development deployment
    docker-compose up -d --build
)

REM Wait for services to start
echo ⏳ Waiting for services to start...
timeout /t 10 /nobreak >nul

REM Health check
echo 🏥 Performing health check...
curl -f http://localhost:8080/health >nul 2>&1
if %errorlevel%==0 (
    echo ✅ Health check passed - server is running
) else (
    echo ❌ Health check failed - checking logs...
    docker-compose logs tasklist-mcp
    exit /b 1
)

echo 🎉 Deployment completed successfully!
echo 📊 Server is available at: http://localhost:8080

REM Show running containers
echo 🐳 Running containers:
docker-compose ps

endlocal

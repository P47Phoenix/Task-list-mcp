@echo off
REM Docker Hub Push Script for Task List MCP Server (Windows)
REM Usage: push-to-dockerhub.bat [dockerhub-username]

setlocal enabledelayedexpansion

set DOCKERHUB_USERNAME=%1
if "%DOCKERHUB_USERNAME%"=="" set DOCKERHUB_USERNAME=your-dockerhub-username

set IMAGE_NAME=tasklist-mcp
set VERSION=latest
set FULL_IMAGE_NAME=%DOCKERHUB_USERNAME%/%IMAGE_NAME%:%VERSION%

echo 🐳 Building and pushing Task List MCP Server to Docker Hub
echo 📦 Image: %FULL_IMAGE_NAME%

REM Check if Docker is running
echo 🔍 Checking Docker status...
docker info >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ Docker is not running. Please start Docker Desktop and try again.
    pause
    exit /b 1
)

REM Login to Docker Hub (prompt user to login manually)
echo 🔐 Please ensure you're logged in to Docker Hub
echo 🔑 If not logged in, run: docker login
pause

REM Build the Docker image
echo 🔨 Building Docker image...
docker build -t %IMAGE_NAME%:%VERSION% .
if %errorlevel% neq 0 (
    echo ❌ Build failed
    pause
    exit /b 1
)

REM Tag the image for Docker Hub
echo 🏷️  Tagging image for Docker Hub...
docker tag %IMAGE_NAME%:%VERSION% %FULL_IMAGE_NAME%

REM Also create a versioned tag with current date
for /f "tokens=2 delims==" %%a in ('wmic OS Get localdatetime /value') do set "dt=%%a"
set "YY=%dt:~2,2%" & set "YYYY=%dt:~0,4%" & set "MM=%dt:~4,2%" & set "DD=%dt:~6,2%"
set DATE_TAG=%DOCKERHUB_USERNAME%/%IMAGE_NAME%:%YYYY%%MM%%DD%
docker tag %IMAGE_NAME%:%VERSION% %DATE_TAG%

REM Push to Docker Hub
echo 📤 Pushing to Docker Hub...
docker push %FULL_IMAGE_NAME%
if %errorlevel% neq 0 (
    echo ❌ Push failed
    pause
    exit /b 1
)

docker push %DATE_TAG%
if %errorlevel% neq 0 (
    echo ❌ Push of dated tag failed
    pause
    exit /b 1
)

echo 🎉 Successfully pushed to Docker Hub!
echo 📋 Available images:
echo    - %FULL_IMAGE_NAME%
echo    - %DATE_TAG%
echo.
echo 🚀 To run the container:
echo    docker run -d -p 8080:8080 %FULL_IMAGE_NAME%
echo.
echo 🐙 To use in docker-compose, update your compose file:
echo    image: %FULL_IMAGE_NAME%

pause
endlocal

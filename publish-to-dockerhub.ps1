#!/usr/bin/env pwsh
# Docker Hub Publisher for Task List MCP Server
# This script will help you publish your Docker image to Docker Hub

$ErrorActionPreference = "Stop"

# Colors for output
$Green = "`e[32m"
$Blue = "`e[34m"
$Yellow = "`e[33m"
$Red = "`e[31m"
$Reset = "`e[0m"

function Write-ColorOutput {
    param($Color, $Message)
    Write-Host "$Color$Message$Reset"
}

Write-ColorOutput $Blue "🚀 Task List MCP Server - Docker Hub Publisher"
Write-ColorOutput $Blue "=============================================="
Write-Host ""

# Check if Docker is running
try {
    $dockerVersion = docker version --format "{{.Server.Version}}" 2>$null
    if (-not $dockerVersion) {
        throw "Docker daemon not responding"
    }
    Write-ColorOutput $Green "✅ Docker is running (version $dockerVersion)"
} catch {
    Write-ColorOutput $Red "❌ Error: Docker is not running or not accessible"
    Write-ColorOutput $Yellow "Please start Docker Desktop and try again"
    exit 1
}

# Check if local image exists
try {
    $imageExists = docker images tasklist-mcp:latest --format "{{.Repository}}" 2>$null
    if (-not $imageExists) {
        Write-ColorOutput $Red "❌ Error: Local image 'tasklist-mcp:latest' not found"
        Write-ColorOutput $Yellow "Please build the image first: docker build -t tasklist-mcp:latest ."
        exit 1
    }
    Write-ColorOutput $Green "✅ Local image 'tasklist-mcp:latest' found"
} catch {
    Write-ColorOutput $Red "❌ Error: Failed to check local image"
    exit 1
}

# Get Docker Hub username
Write-Host ""
Write-ColorOutput $Blue "📋 Please enter your Docker Hub username:"
$DockerHubUsername = Read-Host "Username"

if ([string]::IsNullOrWhiteSpace($DockerHubUsername)) {
    Write-ColorOutput $Red "❌ Error: Docker Hub username is required"
    exit 1
}

# Get version (optional)
Write-Host ""
Write-ColorOutput $Blue "📋 Enter version tag (default: 1.0.0):"
$Version = Read-Host "Version"
if ([string]::IsNullOrWhiteSpace($Version)) {
    $Version = "1.0.0"
}

Write-Host ""
Write-ColorOutput $Yellow "🔍 Summary:"
Write-ColorOutput $Yellow "  Docker Hub Repository: $DockerHubUsername/tasklist-mcp"
Write-ColorOutput $Yellow "  Tags to push: latest, $Version"
Write-Host ""

$confirm = Read-Host "Continue with publishing? (y/N)"
if ($confirm -notmatch "^[yY]$") {
    Write-ColorOutput $Yellow "Publishing cancelled."
    exit 0
}

# Ensure logged in to Docker Hub
Write-ColorOutput $Blue "🔐 Checking Docker Hub authentication..."
try {
    $loginCheck = docker info 2>$null | Select-String "Username"
    if (-not $loginCheck) {
        Write-ColorOutput $Yellow "You need to login to Docker Hub first"
        docker login
        if ($LASTEXITCODE -ne 0) {
            throw "Docker login failed"
        }
    }
    Write-ColorOutput $Green "✅ Docker Hub authentication confirmed"
} catch {
    Write-ColorOutput $Red "❌ Error: Docker Hub authentication failed"
    exit 1
}

# Tag images
$repoName = "$DockerHubUsername/tasklist-mcp"
$tags = @("$repoName`:latest", "$repoName`:$Version")

Write-ColorOutput $Blue "🏷️  Tagging images..."
foreach ($tag in $tags) {
    Write-ColorOutput $Blue "   Creating tag: $tag"
    docker tag tasklist-mcp:latest $tag
    if ($LASTEXITCODE -ne 0) {
        Write-ColorOutput $Red "❌ Error: Failed to tag image as $tag"
        exit 1
    }
}

Write-ColorOutput $Green "✅ Successfully tagged images"

# Push images
Write-ColorOutput $Blue "⬆️  Pushing images to Docker Hub..."
Write-Host ""

foreach ($tag in $tags) {
    Write-ColorOutput $Blue "📤 Pushing $tag..."
    docker push $tag
    if ($LASTEXITCODE -ne 0) {
        Write-ColorOutput $Red "❌ Error: Failed to push $tag"
        exit 1
    }
    Write-ColorOutput $Green "   ✅ Successfully pushed $tag"
    Write-Host ""
}

Write-ColorOutput $Green "🎉 Publishing completed successfully!"
Write-Host ""
Write-ColorOutput $Blue "📦 Your Task List MCP Server is now available on Docker Hub:"
foreach ($tag in $tags) {
    Write-ColorOutput $Blue "   • docker pull $tag"
}

Write-Host ""
Write-ColorOutput $Blue "🚀 To run the container:"
Write-ColorOutput $Blue "   docker run -d -p 8080:8080 -v tasklist_data:/app/data $($tags[0])"
Write-Host ""
Write-ColorOutput $Blue "🐙 To use with docker-compose:"
Write-ColorOutput $Blue "   Update your docker-compose.yml image field to: $($tags[0])"
Write-Host ""
Write-ColorOutput $Blue "📖 For more information, see the documentation in the docs folder"

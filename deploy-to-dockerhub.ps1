#!/usr/bin/env pwsh
param(
    [Parameter(Mandatory=$true)]
    [string]$DockerHubUsername,
    
    [Parameter(Mandatory=$false)]
    [string]$Version = "1.0.0",
    
    [Parameter(Mandatory=$false)]
    [switch]$PushLatest = $true,
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipLogin = $false
)

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

Write-ColorOutput $Blue "🚀 Task List MCP Server - Docker Hub Deployment"
Write-ColorOutput $Blue "================================================="

# Validate parameters
if ([string]::IsNullOrWhiteSpace($DockerHubUsername)) {
    Write-ColorOutput $Red "❌ Error: DockerHubUsername is required"
    exit 1
}

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

# Docker Hub login
if (-not $SkipLogin) {
    Write-ColorOutput $Blue "🔐 Logging into Docker Hub..."
    try {
        docker login
        if ($LASTEXITCODE -ne 0) {
            throw "Docker login failed"
        }
        Write-ColorOutput $Green "✅ Successfully logged into Docker Hub"
    } catch {
        Write-ColorOutput $Red "❌ Error: Docker Hub login failed"
        exit 1
    }
}

# Tag images
$repoName = "$DockerHubUsername/tasklist-mcp"
$tags = @()

if ($PushLatest) {
    $latestTag = "$repoName`:latest"
    $tags += $latestTag
    Write-ColorOutput $Blue "🏷️  Tagging image as $latestTag"
    docker tag tasklist-mcp:latest $latestTag
    if ($LASTEXITCODE -ne 0) {
        Write-ColorOutput $Red "❌ Error: Failed to tag image as latest"
        exit 1
    }
}

if ($Version) {
    $versionTag = "$repoName`:$Version"
    $tags += $versionTag
    Write-ColorOutput $Blue "🏷️  Tagging image as $versionTag"
    docker tag tasklist-mcp:latest $versionTag
    if ($LASTEXITCODE -ne 0) {
        Write-ColorOutput $Red "❌ Error: Failed to tag image with version $Version"
        exit 1
    }
}

Write-ColorOutput $Green "✅ Successfully tagged images"

# Push images
Write-ColorOutput $Blue "⬆️  Pushing images to Docker Hub..."

foreach ($tag in $tags) {
    Write-ColorOutput $Blue "   Pushing $tag..."
    docker push $tag
    if ($LASTEXITCODE -ne 0) {
        Write-ColorOutput $Red "❌ Error: Failed to push $tag"
        exit 1
    }
    Write-ColorOutput $Green "   ✅ Successfully pushed $tag"
}

Write-ColorOutput $Green "🎉 Deployment completed successfully!"
Write-ColorOutput $Blue ""
Write-ColorOutput $Blue "📦 Your Task List MCP Server is now available on Docker Hub:"

foreach ($tag in $tags) {
    Write-ColorOutput $Blue "   • docker pull $tag"
}

Write-ColorOutput $Blue ""
Write-ColorOutput $Blue "🚀 To run the container:"
Write-ColorOutput $Blue "   docker run -p 8080:8080 $($tags[0])"
Write-ColorOutput $Blue ""
Write-ColorOutput $Blue "📖 For more information, see DOCKER-HUB.md"

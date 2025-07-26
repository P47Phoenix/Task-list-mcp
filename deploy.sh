#!/bin/bash

# Task List MCP Server Deployment Script
# Usage: ./deploy.sh [environment]

set -e

ENVIRONMENT=${1:-development}
COMPOSE_FILE="docker-compose.yml"

echo "🚀 Deploying Task List MCP Server (Environment: $ENVIRONMENT)"

# Load environment-specific variables
if [ -f ".env.$ENVIRONMENT" ]; then
    echo "📝 Loading environment configuration from .env.$ENVIRONMENT"
    export $(cat .env.$ENVIRONMENT | grep -v '^#' | xargs)
else
    echo "⚠️  No environment file found for $ENVIRONMENT, using defaults"
fi

# Build and deploy based on environment
case $ENVIRONMENT in
    "production")
        echo "🏭 Production deployment"
        docker-compose --profile production up -d --build
        ;;
    "staging")
        echo "🧪 Staging deployment"
        docker-compose up -d --build
        ;;
    "development"|*)
        echo "🛠️  Development deployment"
        docker-compose up -d --build
        ;;
esac

# Wait for services to be ready
echo "⏳ Waiting for services to start..."
sleep 10

# Health check
echo "🏥 Performing health check..."
if curl -f http://localhost:8080/health > /dev/null 2>&1; then
    echo "✅ Health check passed - server is running"
else
    echo "❌ Health check failed - checking logs..."
    docker-compose logs tasklist-mcp
    exit 1
fi

echo "🎉 Deployment completed successfully!"
echo "📊 Server is available at: http://localhost:8080"

# Show running containers
echo "🐳 Running containers:"
docker-compose ps

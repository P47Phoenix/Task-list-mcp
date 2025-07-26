#!/bin/bash

# Docker Hub Push Script for Task List MCP Server
# Usage: ./push-to-dockerhub.sh [dockerhub-username]

set -e

DOCKERHUB_USERNAME=${1:-"your-dockerhub-username"}
IMAGE_NAME="tasklist-mcp"
VERSION="latest"
FULL_IMAGE_NAME="${DOCKERHUB_USERNAME}/${IMAGE_NAME}:${VERSION}"

echo "🐳 Building and pushing Task List MCP Server to Docker Hub"
echo "📦 Image: ${FULL_IMAGE_NAME}"

# Check if Docker is running
echo "🔍 Checking Docker status..."
if ! docker info > /dev/null 2>&1; then
    echo "❌ Docker is not running. Please start Docker Desktop and try again."
    exit 1
fi

# Login to Docker Hub (if not already logged in)
echo "🔐 Checking Docker Hub authentication..."
if ! docker system info | grep -q "Username:"; then
    echo "🔑 Please log in to Docker Hub:"
    docker login
else
    echo "✅ Already logged in to Docker Hub"
fi

# Build the Docker image
echo "🔨 Building Docker image..."
docker build -t ${IMAGE_NAME}:${VERSION} .

# Tag the image for Docker Hub
echo "🏷️  Tagging image for Docker Hub..."
docker tag ${IMAGE_NAME}:${VERSION} ${FULL_IMAGE_NAME}

# Also create a versioned tag with current date
DATE_TAG="${DOCKERHUB_USERNAME}/${IMAGE_NAME}:$(date +%Y%m%d)"
docker tag ${IMAGE_NAME}:${VERSION} ${DATE_TAG}

# Push to Docker Hub
echo "📤 Pushing to Docker Hub..."
docker push ${FULL_IMAGE_NAME}
docker push ${DATE_TAG}

echo "🎉 Successfully pushed to Docker Hub!"
echo "📋 Available images:"
echo "   - ${FULL_IMAGE_NAME}"
echo "   - ${DATE_TAG}"
echo ""
echo "🚀 To run the container:"
echo "   docker run -d -p 8080:8080 ${FULL_IMAGE_NAME}"
echo ""
echo "🐙 To use in docker-compose, update your compose file:"
echo "   image: ${FULL_IMAGE_NAME}"

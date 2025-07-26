# Docker Hub Deployment Instructions

## Prerequisites

1. **Start Docker Desktop** on Windows
2. **Create Docker Hub account** if you don't have one at https://hub.docker.com
3. **Login to Docker Hub** from command line

## Quick Deploy Steps

### Step 1: Start Docker Desktop
Make sure Docker Desktop is running (you should see the Docker whale icon in your system tray).

### Step 2: Login to Docker Hub
```bash
docker login
```
Enter your Docker Hub username and password when prompted.

### Step 3: Build the Image
```bash
# Replace 'yourusername' with your actual Docker Hub username
docker build -t yourusername/tasklist-mcp:latest .
```

### Step 4: Push to Docker Hub
```bash
# Replace 'yourusername' with your actual Docker Hub username
docker push yourusername/tasklist-mcp:latest
```

### Step 5: Verify Upload
Go to https://hub.docker.com and check your repositories to confirm the upload.

## Automated Scripts

### Windows
```cmd
push-to-dockerhub.bat yourusername
```

### Linux/macOS
```bash
chmod +x push-to-dockerhub.sh
./push-to-dockerhub.sh yourusername
```

## Using the Published Image

### Docker Run
```bash
docker run -d -p 8080:8080 yourusername/tasklist-mcp:latest
```

### Docker Compose
Update your `docker-compose.yml`:
```yaml
services:
  tasklist-mcp:
    image: yourusername/tasklist-mcp:latest
    # ... rest of configuration
```

### Kubernetes
Update your deployment:
```yaml
spec:
  containers:
  - name: tasklist-mcp
    image: yourusername/tasklist-mcp:latest
```

## Image Details

- **Base Image**: mcr.microsoft.com/dotnet/aspnet:8.0
- **Platform**: linux/amd64
- **Size**: ~200MB (optimized multi-stage build)
- **Health Check**: Included (`/health` endpoint)
- **Security**: Non-root user, minimal surface area
- **Environment**: Production-ready with monitoring

## Tags

The scripts create two tags:
- `latest` - Always points to the most recent build
- `YYYYMMDD` - Date-stamped version for rollback capability

## Troubleshooting

### Docker not running
```
Error: docker daemon not running
Solution: Start Docker Desktop
```

### Authentication failed
```
Error: unauthorized
Solution: Run 'docker login' and enter credentials
```

### Build fails
```
Error: Build failed
Solution: Ensure all files are present and .NET build succeeds locally
```

### Push fails
```
Error: Push failed
Solution: Check internet connection and Docker Hub status
```

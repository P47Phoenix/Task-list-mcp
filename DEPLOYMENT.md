# Task List MCP Server - Deployment Guide

This guide covers various deployment options for the Task List MCP Server, from development to production environments.

## Quick Start

### Development (Local)

1. **Prerequisites**
   - .NET 8.0 SDK
   - SQLite (included with .NET)

2. **Run Locally**
   ```bash
   cd src/TaskListMcp.Server
   dotnet run
   ```

3. **Access**
   - Server: http://localhost:8080
   - Health Check: http://localhost:8080/health

### Docker (Recommended)

1. **Prerequisites**
   - Docker and Docker Compose

2. **Quick Deploy**
   ```bash
   # Linux/macOS
   chmod +x deploy.sh
   ./deploy.sh

   # Windows
   deploy.bat
   ```

3. **Manual Docker Commands**
   ```bash
   # Build and run
   docker-compose up -d --build

   # View logs
   docker-compose logs -f

   # Stop
   docker-compose down
   ```

## Deployment Options

### 1. Docker Compose (Development/Staging)

**File**: `docker-compose.yml`

```bash
# Development
docker-compose up -d

# Production with nginx
docker-compose --profile production up -d
```

**Features**:
- Automatic health checks
- Volume persistence
- Environment configuration
- Optional nginx reverse proxy

### 2. Kubernetes (Production)

**File**: `k8s-deployment.yml`

```bash
# Deploy to Kubernetes
kubectl apply -f k8s-deployment.yml

# Check status
kubectl get pods -l app=tasklist-mcp
kubectl get svc tasklist-mcp-service
```

**Features**:
- High availability (2 replicas)
- Auto-scaling ready
- Persistent storage
- Ingress with SSL
- Health checks and monitoring

### 3. Cloud Platforms

#### Azure Container Instances

```bash
# Create resource group
az group create --name tasklist-rg --location eastus

# Deploy container
az container create \
  --resource-group tasklist-rg \
  --name tasklist-mcp \
  --image tasklist-mcp:latest \
  --cpu 1 --memory 1 \
  --ports 8080 \
  --environment-variables ASPNETCORE_ENVIRONMENT=Production
```

#### AWS ECS/Fargate

```bash
# Create task definition and service
aws ecs create-task-definition --cli-input-json file://ecs-task-definition.json
aws ecs create-service --cluster default --service-name tasklist-mcp --task-definition tasklist-mcp
```

#### Google Cloud Run

```bash
# Build and deploy
gcloud builds submit --tag gcr.io/PROJECT-ID/tasklist-mcp
gcloud run deploy --image gcr.io/PROJECT-ID/tasklist-mcp --platform managed
```

## Configuration

### Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `TASKLIST_DB_PATH` | `./data/tasks.db` | Database file path |
| `ASPNETCORE_ENVIRONMENT` | `Development` | Environment (Development/Production) |
| `ASPNETCORE_URLS` | `http://localhost:8080` | Server URLs |
| `MAX_QUERY_RESULTS` | `1000` | Maximum query results |
| `QUERY_TIMEOUT_SECONDS` | `30` | Database query timeout |
| `ENABLE_HEALTH_CHECKS` | `true` | Enable health endpoints |
| `ENABLE_SWAGGER` | `true` | Enable API documentation |

### Configuration Files

1. **Development**: `.env.example` → `.env`
2. **Production**: `.env.production`
3. **Docker**: Environment variables in `docker-compose.yml`
4. **Kubernetes**: ConfigMap or environment variables

## Storage and Persistence

### Docker Volumes

```yaml
volumes:
  tasklist_data:
    driver: local
```

### Kubernetes Persistent Volumes

```yaml
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: tasklist-data-pvc
spec:
  accessModes:
    - ReadWriteOnce
  resources:
    requests:
      storage: 10Gi
```

### Database Backup

```bash
# Manual backup
sqlite3 tasks.db ".backup backup-$(date +%Y%m%d).db"

# Automated backup script
./scripts/backup-database.sh
```

## Monitoring and Health Checks

### Health Endpoints

- **Basic**: `GET /health`
- **Detailed**: `GET /health/detailed`

### Health Check Response

```json
{
  "status": "healthy",
  "timestamp": "2025-07-26T10:00:00Z",
  "version": "1.0.0",
  "database": "connected"
}
```

### Metrics Integration

- **Prometheus**: Metrics available at `/metrics` (if enabled)
- **Application Insights**: Azure monitoring
- **CloudWatch**: AWS monitoring
- **Stackdriver**: Google Cloud monitoring

## Security Considerations

### Production Security

1. **Environment Variables**
   ```bash
   ASPNETCORE_ENVIRONMENT=Production
   ENABLE_SWAGGER=false
   REQUIRE_AUTHENTICATION=true
   ```

2. **Network Security**
   - Use HTTPS in production
   - Configure firewall rules
   - Implement rate limiting

3. **Database Security**
   - Set appropriate file permissions
   - Regular backups
   - Encryption at rest (if required)

### SSL/TLS Configuration

1. **Docker with nginx**
   - Place certificates in `./ssl/` directory
   - Update `nginx.conf` with SSL configuration

2. **Kubernetes**
   - Use cert-manager for automatic SSL
   - Configure ingress with TLS

## Scaling and Performance

### Horizontal Scaling

**Kubernetes**:
```yaml
spec:
  replicas: 3  # Scale to 3 instances
```

**Docker Swarm**:
```bash
docker service scale tasklist-mcp=3
```

### Vertical Scaling

**Resource Limits**:
```yaml
resources:
  requests:
    memory: "256Mi"
    cpu: "250m"
  limits:
    memory: "512Mi"
    cpu: "500m"
```

### Performance Tuning

1. **Database Optimization**
   - Enable query caching
   - Configure connection pooling
   - Regular VACUUM operations

2. **Application Settings**
   ```bash
   ENABLE_QUERY_CACHING=true
   MAX_CONCURRENT_REQUESTS=100
   REQUEST_TIMEOUT_SECONDS=60
   ```

## Troubleshooting

### Common Issues

1. **Database Connection Errors**
   ```bash
   # Check file permissions
   ls -la data/tasks.db
   
   # Verify database integrity
   sqlite3 data/tasks.db "PRAGMA integrity_check;"
   ```

2. **Memory Issues**
   ```bash
   # Check container memory usage
   docker stats tasklist-mcp-server
   
   # Increase memory limits
   docker-compose down
   # Edit docker-compose.yml memory limits
   docker-compose up -d
   ```

3. **Performance Issues**
   ```bash
   # Check detailed health status
   curl http://localhost:8080/health/detailed
   
   # Review logs
   docker-compose logs tasklist-mcp
   ```

### Log Analysis

```bash
# View real-time logs
docker-compose logs -f tasklist-mcp

# Search for errors
docker-compose logs tasklist-mcp | grep -i error

# Export logs
docker-compose logs tasklist-mcp > tasklist-logs.txt
```

## Maintenance

### Regular Tasks

1. **Database Maintenance**
   - Weekly: Check database size and performance
   - Monthly: Run VACUUM to optimize database
   - Quarterly: Review and clean old data

2. **Updates**
   ```bash
   # Pull latest image
   docker-compose pull
   
   # Restart with new image
   docker-compose up -d
   ```

3. **Backups**
   ```bash
   # Automated daily backup
   0 2 * * * /path/to/backup-script.sh
   ```

### Monitoring Checklist

- [ ] Health endpoints responding
- [ ] Database connectivity
- [ ] Memory usage within limits
- [ ] Disk space available
- [ ] Response times acceptable
- [ ] Error rates low
- [ ] Backup files created

## Support

For deployment issues:

1. Check the health endpoint: `/health/detailed`
2. Review application logs
3. Verify configuration settings
4. Check resource availability
5. Consult troubleshooting section

For additional help, see the main README.md or create an issue in the repository.

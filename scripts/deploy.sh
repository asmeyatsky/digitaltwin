#!/bin/bash

# Digital Twin Deployment Script
# Usage: ./deploy.sh [environment] [version]

set -e

# Default values
ENVIRONMENT=${1:-staging}
VERSION=${2:-latest}
REGION=${AWS_REGION:-us-west-2}

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Logging functions
log_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Function to check prerequisites
check_prerequisites() {
    log_info "Checking prerequisites..."
    
    # Check AWS CLI
    if ! command -v aws &> /dev/null; then
        log_error "AWS CLI is not installed"
        exit 1
    fi
    
    # Check kubectl
    if ! command -v kubectl &> /dev/null; then
        log_error "kubectl is not installed"
        exit 1
    fi
    
    # Check Docker
    if ! command -v docker &> /dev/null; then
        log_error "Docker is not installed"
        exit 1
    fi
    
    # Check Helm
    if ! command -v helm &> /dev/null; then
        log_error "Helm is not installed"
        exit 1
    fi
    
    log_info "All prerequisites are installed"
}

# Function to configure AWS credentials
configure_aws() {
    log_info "Configuring AWS credentials..."
    
    if [ -z "$AWS_PROFILE" ]; then
        aws configure set aws_access_key_id "$AWS_ACCESS_KEY_ID"
        aws configure set aws_secret_access_key "$AWS_SECRET_ACCESS_KEY"
        aws configure set default.region "$REGION"
    else
        export AWS_PROFILE="$AWS_PROFILE"
    fi
    
    # Update kubeconfig
    aws eks update-kubeconfig --name "digitaltwin-${ENVIRONMENT}" --region "$REGION"
}

# Function to build and push Docker image
build_and_push_image() {
    log_info "Building and pushing Docker image..."
    
    local image_name="digitaltwin/api"
    local tag="${VERSION}"
    
    # Build Docker image
    docker build -t "${image_name}:${tag}" .
    docker tag "${image_name}:${tag}" "${image_name}:latest"
    
    # Push to registry
    docker push "${image_name}:${tag}"
    docker push "${image_name}:latest"
}

# Function to deploy infrastructure
deploy_infrastructure() {
    log_info "Deploying infrastructure..."
    
    # Create namespace if it doesn't exist
    kubectl create namespace "digitaltwin-${ENVIRONMENT}" --dry-run=client -o yaml | kubectl apply -f -
    
    # Deploy infrastructure components
    kubectl apply -f k8s/infrastructure.yaml -n "digitaltwin-${ENVIRONMENT}"
    
    # Wait for infrastructure to be ready
    log_info "Waiting for infrastructure to be ready..."
    kubectl wait --for=condition=available --timeout=600s deployment/digitaltwin-postgres -n "digitaltwin-${ENVIRONMENT}"
    kubectl wait --for=condition=available --timeout=600s deployment/digitaltwin-redis -n "digitaltwin-${ENVIRONMENT}"
    kubectl wait --for=condition=available --timeout=600s deployment/digitaltwin-rabbitmq -n "digitaltwin-${ENVIRONMENT}"
}

# Function to deploy application
deploy_application() {
    log_info "Deploying application..."
    
    # Update image tag in deployment
    sed -i "s|digitaltwin/api:latest|digitaltwin/api:${VERSION}|g" k8s/api-deployment.yaml
    
    # Deploy application
    kubectl apply -f k8s/api-deployment.yaml -n "digitaltwin-${ENVIRONMENT}"
    
    # Wait for deployment to be ready
    log_info "Waiting for application to be ready..."
    kubectl wait --for=condition=available --timeout=600s deployment/digitaltwin-api -n "digitaltwin-${ENVIRONMENT}"
}

# Function to run database migrations
run_migrations() {
    log_info "Running database migrations..."
    
    # Get database connection string from secret
    local connection_string=$(kubectl get secret digitaltwin-secrets -n "digitaltwin-${ENVIRONMENT}" -o jsonpath='{.data.database-connection}' | base64 -d)
    
    # Run migrations
    kubectl run migration --image=digitaltwin/api:${VERSION} --rm -i --restart=Never \
        --env="ConnectionStrings__DefaultConnection=${connection_string}" \
        -n "digitaltwin-${ENVIRONMENT}" \
        -- dotnet ef database update
}

# Function to run health checks
run_health_checks() {
    log_info "Running health checks..."
    
    local url="http://digitaltwin-${ENVIRONMENT}.com/health"
    
    if [ "$ENVIRONMENT" = "local" ]; then
        url="http://localhost:8080/health"
    fi
    
    # Wait for health endpoint
    local max_attempts=30
    local attempt=1
    
    while [ $attempt -le $max_attempts ]; do
        if curl -f "$url" > /dev/null 2>&1; then
            log_info "Health check passed!"
            return 0
        fi
        
        log_warn "Health check attempt $attempt failed, retrying in 10 seconds..."
        sleep 10
        ((attempt++))
    done
    
    log_error "Health check failed after $max_attempts attempts"
    return 1
}

# Function to run smoke tests
run_smoke_tests() {
    log_info "Running smoke tests..."
    
    local base_url="http://digitaltwin-${ENVIRONMENT}.com"
    
    if [ "$ENVIRONMENT" = "local" ]; then
        base_url="http://localhost:8080"
    fi
    
    # Test API endpoints
    curl -f "${base_url}/health" || { log_error "Health endpoint test failed"; return 1; }
    curl -f "${base_url}/api/health" || { log_error "API health endpoint test failed"; return 1; }
    
    log_info "Smoke tests passed!"
}

# Function to rollback
rollback() {
    log_warn "Rolling back deployment..."
    
    kubectl rollout undo deployment/digitaltwin-api -n "digitaltwin-${ENVIRONMENT}"
    kubectl rollout status deployment/digitaltwin-api -n "digitaltwin-${ENVIRONMENT}"
    
    log_info "Rollback completed"
}

# Function to get deployment status
get_status() {
    log_info "Getting deployment status for ${ENVIRONMENT}..."
    
    kubectl get pods -n "digitaltwin-${ENVIRONMENT}" -l app=digitaltwin-api
    kubectl get deployment/digitaltwin-api -n "digitaltwin-${ENVIRONMENT}"
    kubectl get service/digitaltwin-api-service -n "digitaltwin-${ENVIRONMENT}"
}

# Function to cleanup
cleanup() {
    log_info "Cleaning up..."
    
    # Delete old Docker images
    docker system prune -f --filter "until=240h"
    
    # Remove unused Helm releases
    helm list --namespace "digitaltwin-${ENVIRONMENT}" -q | xargs helm uninstall --namespace "digitaltwin-${ENVIRONMENT}"
    
    log_info "Cleanup completed"
}

# Function to monitor deployment
monitor_deployment() {
    log_info "Starting deployment monitoring..."
    
    # Watch deployment status
    kubectl rollout status deployment/digitaltwin-api -n "digitaltwin-${ENVIRONMENT}" -w
    
    # Show logs
    kubectl logs -f deployment/digitaltwin-api -n "digitaltwin-${ENVIRONMENT}"
}

# Main deployment function
deploy() {
    log_info "Starting deployment to ${ENVIRONMENT} environment..."
    
    check_prerequisites
    configure_aws
    build_and_push_image
    deploy_infrastructure
    deploy_application
    run_migrations
    run_health_checks
    run_smoke_tests
    
    log_info "Deployment to ${ENVIRONMENT} completed successfully!"
}

# Script usage
usage() {
    echo "Usage: $0 [command] [options]"
    echo ""
    echo "Commands:"
    echo "  deploy [environment] [version]     Deploy to specified environment"
    echo "  rollback [environment]              Rollback deployment"
    echo "  status [environment]                 Show deployment status"
    echo "  cleanup [environment]                Cleanup resources"
    echo "  monitor [environment]               Monitor deployment"
    echo "  health [environment]                Run health checks"
    echo "  test [environment]                  Run smoke tests"
    echo "  prerequisites                        Check prerequisites"
    echo ""
    echo "Environments: local, staging, production"
    echo ""
    echo "Examples:"
    echo "  $0 deploy staging v1.0.0"
    echo "  $0 rollback production"
    echo "  $0 status staging"
    echo "  $0 monitor production"
}

# Main script logic
case "${1:-help}" in
    "deploy")
        ENVIRONMENT="${2:-staging}"
        VERSION="${3:-latest}"
        deploy
        ;;
    "rollback")
        ENVIRONMENT="${2:-staging}"
        configure_aws
        rollback
        ;;
    "status")
        ENVIRONMENT="${2:-staging}"
        configure_aws
        get_status
        ;;
    "cleanup")
        ENVIRONMENT="${2:-staging}"
        configure_aws
        cleanup
        ;;
    "monitor")
        ENVIRONMENT="${2:-staging}"
        configure_aws
        monitor_deployment
        ;;
    "health")
        ENVIRONMENT="${2:-staging}"
        run_health_checks
        ;;
    "test")
        ENVIRONMENT="${2:-staging}"
        run_smoke_tests
        ;;
    "prerequisites")
        check_prerequisites
        ;;
    "help"|*)
        usage
        exit 0
        ;;
esac

exit 0
#!/bin/bash

# Digital Twin CI/CD Helper Script
# This script provides utility functions for the CI/CD pipeline

set -e

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
ENVIRONMENT_FILE="$PROJECT_ROOT/.env"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
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

log_debug() {
    if [ "$DEBUG" = "true" ]; then
        echo -e "${BLUE}[DEBUG]${NC} $1"
    fi
}

# Load environment variables
load_env() {
    if [ -f "$ENVIRONMENT_FILE" ]; then
        export $(cat "$ENVIRONMENT_FILE" | grep -v '^#' | xargs)
        log_info "Environment variables loaded from $ENVIRONMENT_FILE"
    else
        log_warn "No environment file found at $ENVIRONMENT_FILE"
    fi
}

# Function to validate environment
validate_environment() {
    local env=${1:-staging}
    
    case $env in
        local|staging|production)
            log_info "Environment $env is valid"
            ;;
        *)
            log_error "Invalid environment: $env. Must be local, staging, or production"
            exit 1
            ;;
    esac
}

# Function to get current branch
get_current_branch() {
    git rev-parse --abbrev-ref HEAD
}

# Function to get commit hash
get_commit_hash() {
    git rev-parse HEAD
}

# Function to get version
get_version() {
    local branch=$(get_current_branch)
    local commit_hash=$(get_commit_hash)
    local timestamp=$(date +%Y%m%d%H%M%S)
    
    if [ "$branch" = "main" ] || [ "$branch" = "master" ]; then
        echo "latest"
    else
        echo "${branch}-${commit_hash:0:7}-${timestamp}"
    fi
}

# Function to check if branch is main/master
is_main_branch() {
    local branch=$(get_current_branch)
    [ "$branch" = "main" ] || [ "$branch" = "master" ]
}

# Function to get pull request number
get_pr_number() {
    if [ "$GITHUB_EVENT_NAME" = "pull_request" ]; then
        echo "$GITHUB_REF"
    else
        echo ""
    fi
}

# Function to generate build metadata
generate_build_metadata() {
    local metadata_file="$PROJECT_ROOT/build-metadata.json"
    
    cat > "$metadata_file" << EOF
{
    "build_number": "$GITHUB_RUN_NUMBER",
    "commit": "$(get_commit_hash)",
    "branch": "$(get_current_branch)",
    "version": "$(get_version)",
    "timestamp": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
    "pull_request": "$(get_pr_number)",
    "environment": "$DEPLOYMENT_ENVIRONMENT",
    "build_url": "$GITHUB_SERVER_URL/$GITHUB_REPOSITORY/actions/runs/$GITHUB_RUN_ID"
}
EOF
    
    log_info "Build metadata saved to $metadata_file"
}

# Function to check Docker daemon
check_docker_daemon() {
    if ! docker info > /dev/null 2>&1; then
        log_error "Docker daemon is not running"
        exit 1
    fi
}

# Function to build Docker image with proper tagging
build_docker_image() {
    local image_name=$1
    local tag=$2
    local dockerfile=${3:-Dockerfile}
    
    log_info "Building Docker image: $image_name:$tag"
    
    check_docker_daemon
    
    # Build with build metadata
    docker build \
        --file "$dockerfile" \
        --build-arg BUILD_VERSION="$tag" \
        --build-arg BUILD_DATE="$(date -u +%Y-%m-%dT%H:%M:%SZ)" \
        --build-arg VCS_REF="$(get_commit_hash)" \
        --tag "$image_name:$tag" \
        --tag "$image_name:latest" \
        .
}

# Function to push Docker image to registry
push_docker_image() {
    local image_name=$1
    local tag=$2
    local registry=${3:-docker.io}
    
    log_info "Pushing Docker image: $registry/$image_name:$tag"
    
    docker push "$registry/$image_name:$tag"
    docker push "$registry/$image_name:latest"
}

# Function to run tests with coverage
run_tests_with_coverage() {
    local test_project=$1
    local coverage_output=$2
    
    log_info "Running tests for $test_project"
    
    dotnet test \
        "$test_project" \
        --logger "console;verbosity=detailed" \
        --collect:"XPlat Code Coverage" \
        --results-directory "$coverage_output" \
        --configuration Release
}

# Function to generate coverage report
generate_coverage_report() {
    local coverage_input=$1
    local coverage_output=$2
    
    log_info "Generating coverage report from $coverage_input"
    
    if ! command -v reportgenerator &> /dev/null; then
        log_warn "ReportGenerator not found, installing..."
        dotnet tool install -g dotnet-reportgenerator-globaltool
    fi
    
    reportgenerator \
        -reports:"$coverage_input/**/coverage.cobertura.xml" \
        -targetdir:"$coverage_output" \
        -reporttypes:Html,Xml,Cobertura,JsonSummary
}

# Function to upload coverage to Codecov
upload_coverage_to_codecov() {
    local coverage_file=$1
    local flags=$2
    
    if [ -z "$CODECOV_TOKEN" ]; then
        log_warn "CODECOV_TOKEN not set, skipping codecov upload"
        return 0
    fi
    
    log_info "Uploading coverage to Codecov"
    
    bash <(curl -s https://codecov.io/bash) \
        -f "$coverage_file" \
        -F "$flags" \
        -t "$CODECOV_TOKEN"
}

# Function to check Kubernetes cluster connectivity
check_k8s_connectivity() {
    log_info "Checking Kubernetes cluster connectivity"
    
    if ! kubectl cluster-info > /dev/null 2>&1; then
        log_error "Cannot connect to Kubernetes cluster"
        exit 1
    fi
    
    log_info "Kubernetes cluster is accessible"
}

# Function to wait for Kubernetes deployment
wait_for_k8s_deployment() {
    local deployment=$1
    local namespace=${2:-default}
    local timeout=${3:-600}
    
    log_info "Waiting for deployment $deployment in namespace $namespace"
    
    kubectl wait \
        --for=condition=available \
        --timeout="${timeout}s" \
        deployment/"$deployment" \
        -n "$namespace"
}

# Function to run Kubernetes rollout status
run_k8s_rollout_status() {
    local deployment=$1
    local namespace=${2:-default}
    
    log_info "Checking rollout status for deployment $deployment"
    
    kubectl rollout status deployment/"$deployment" -n "$namespace"
}

# Function to get Kubernetes pod logs
get_k8s_pod_logs() {
    local deployment=$1
    local namespace=${2:-default}
    local follow=${3:-false}
    local tail_lines=${4:-100}
    
    local pod_name=$(kubectl get pods -n "$namespace" -l app="$deployment" -o jsonpath='{.items[0].metadata.name}')
    
    if [ "$follow" = "true" ]; then
        kubectl logs -f "$pod_name" -n "$namespace"
    else
        kubectl logs "$pod_name" -n "$namespace" --tail="$tail_lines"
    fi
}

# Function to execute commands in Kubernetes pod
exec_in_k8s_pod() {
    local deployment=$1
    local namespace=${2:-default}
    shift 2
    local command="$@"
    
    local pod_name=$(kubectl get pods -n "$namespace" -l app="$deployment" -o jsonpath='{.items[0].metadata.name}')
    
    kubectl exec "$pod_name" -n "$namespace" -- $command
}

# Function to run security scan
run_security_scan() {
    local scan_type=$1
    local target=$2
    
    log_info "Running $scan_type security scan on $target"
    
    case $scan_type in
        "trivy")
            docker run --rm -v /var/run/docker.sock:/var/run/docker.sock \
                aquasecurity/trivy:latest \
                image "$target" \
                --format json \
                --output "trivy-scan-$target.json"
            ;;
        "sast")
            # Run static analysis security testing
            # This would integrate with tools like SonarQube, Snyk, etc.
            log_info "Running SAST scan with SonarCloud"
            ;;
        "dependency")
            # Run dependency vulnerability scanning
            # This would use OWASP Dependency Check or similar
            log_info "Running dependency vulnerability scan"
            ;;
        *)
            log_error "Unknown scan type: $scan_type"
            exit 1
            ;;
    esac
}

# Function to notify deployment
notify_deployment() {
    local status=$1
    local environment=$2
    local message=$3
    local webhook_url=$4
    
    if [ -z "$webhook_url" ]; then
        log_warn "No webhook URL provided, skipping notification"
        return 0
    fi
    
    log_info "Sending deployment notification: $status"
    
    local color="good"
    if [ "$status" = "failed" ] || [ "$status" = "error" ]; then
        color="danger"
    elif [ "$status" = "warning" ]; then
        color="warning"
    fi
    
    curl -X POST "$webhook_url" \
        -H 'Content-type: application/json' \
        -d @- <<EOF
{
    "attachments": [
        {
            "color": "$color",
            "title": "Digital Twin Deployment - $status",
            "fields": [
                {
                    "title": "Environment",
                    "value": "$environment",
                    "short": true
                },
                {
                    "title": "Status",
                    "value": "$status",
                    "short": true
                },
                {
                    "title": "Timestamp",
                    "value": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
                    "short": true
                }
            ],
            "text": "$message"
        }
    ]
}
EOF
}

# Function to cleanup old Docker images
cleanup_old_docker_images() {
    local image_pattern=$1
    local keep_count=${2:-5}
    
    log_info "Cleaning up old Docker images matching pattern: $image_pattern"
    
    # Remove old images, keeping the most recent ones
    docker images "$image_pattern" --format "table {{.Repository}}:{{.Tag}}\t{{.CreatedAt}}" | \
        sort -k2 -r | \
        tail -n +$((keep_count + 1)) | \
        awk '{print $1}' | \
        xargs -r docker rmi -f
}

# Function to backup Kubernetes resources
backup_k8s_resources() {
    local namespace=$1
    local backup_dir=$2
    
    log_info "Creating backup of Kubernetes resources in namespace $namespace"
    
    mkdir -p "$backup_dir"
    
    # Backup all resources in the namespace
    kubectl get all -n "$namespace" -o yaml > "$backup_dir/all-resources.yaml"
    
    # Backup specific resource types
    kubectl get configmaps -n "$namespace" -o yaml > "$backup_dir/configmaps.yaml"
    kubectl get secrets -n "$namespace" -o yaml > "$backup_dir/secrets.yaml"
    kubectl get deployments -n "$namespace" -o yaml > "$backup_dir/deployments.yaml"
    kubectl get services -n "$namespace" -o yaml > "$backup_dir/services.yaml"
    
    log_info "Backup created in $backup_dir"
}

# Function to validate deployment
validate_deployment() {
    local environment=$1
    local base_url=${2:-http://digitaltwin-$environment.com}
    
    log_info "Validating deployment to $environment"
    
    # Health check
    if ! curl -f "$base_url/health" > /dev/null 2>&1; then
        log_error "Health check failed"
        return 1
    fi
    
    # API health check
    if ! curl -f "$base_url/api/health" > /dev/null 2>&1; then
        log_error "API health check failed"
        return 1
    fi
    
    # Check required endpoints are accessible
    local endpoints=("/api/health" "/swagger" "/metrics")
    for endpoint in "${endpoints[@]}"; do
        if ! curl -f "$base_url$endpoint" > /dev/null 2>&1; then
            log_error "Endpoint $endpoint is not accessible"
            return 1
        fi
    done
    
    log_info "Deployment validation passed"
    return 0
}

# Main function to show help
show_help() {
    echo "Digital Twin CI/CD Helper Script"
    echo ""
    echo "Usage: $0 [function] [options]"
    echo ""
    echo "Functions:"
    echo "  load_env                     Load environment variables"
    echo "  validate_environment [env]    Validate environment"
    echo "  get_current_branch           Get current git branch"
    echo "  get_commit_hash              Get current commit hash"
    echo "  get_version                  Get version string"
    echo "  is_main_branch              Check if current branch is main/master"
    echo "  get_pr_number               Get pull request number"
    echo "  generate_build_metadata     Generate build metadata"
    echo "  build_docker_image [name] [tag] [dockerfile]"
    echo "  push_docker_image [name] [tag] [registry]"
    echo "  run_tests_with_coverage [project] [output]"
    echo "  generate_coverage_report [input] [output]"
    echo "  upload_coverage_to_codecov [file] [flags]"
    echo "  check_k8s_connectivity     Check Kubernetes connectivity"
    echo "  wait_for_k8s_deployment [deployment] [namespace] [timeout]"
    echo "  run_k8s_rollout_status [deployment] [namespace]"
    echo "  get_k8s_pod_logs [deployment] [namespace] [follow] [lines]"
    echo "  exec_in_k8s_pod [deployment] [namespace] [command...]"
    echo "  run_security_scan [type] [target]"
    echo "  notify_deployment [status] [env] [message] [webhook]"
    echo "  cleanup_old_docker_images [pattern] [keep_count]"
    echo "  backup_k8s_resources [namespace] [backup_dir]"
    echo "  validate_deployment [environment] [base_url]"
    echo "  show_help                    Show this help message"
}

# Export functions for use in other scripts
export -f log_info log_warn log_error log_debug
export -f load_env validate_environment get_current_branch get_commit_hash get_version
export -f is_main_branch get_pr_number generate_build_metadata
export -f build_docker_image push_docker_image
export -f run_tests_with_coverage generate_coverage_report upload_coverage_to_codecov
export -f check_k8s_connectivity wait_for_k8s_deployment run_k8s_rollout_status
export -f get_k8s_pod_logs exec_in_k8s_pod
export -f run_security_scan notify_deployment cleanup_old_docker_images
export -f backup_k8s_resources validate_deployment

# Main script execution
if [ "${BASH_SOURCE[0]}" = "${0}" ]; then
    case "${1:-help}" in
        "load_env")
            load_env
            ;;
        "validate_environment")
            validate_environment "${2:-staging}"
            ;;
        "get_current_branch")
            get_current_branch
            ;;
        "get_commit_hash")
            get_commit_hash
            ;;
        "get_version")
            get_version
            ;;
        "is_main_branch")
            is_main_branch
            ;;
        "get_pr_number")
            get_pr_number
            ;;
        "generate_build_metadata")
            generate_build_metadata
            ;;
        "show_help"|*)
            show_help
            ;;
    esac
fi
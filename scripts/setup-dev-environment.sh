#!/bin/bash

# Digital Twin Development Environment Setup
# Complete development environment setup for premier emotional companion

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}🏗️  Digital Twin Development Environment Setup${NC}"
echo -e "${BLUE}======================================${NC}"

# Function to print status
print_status() {
    echo -e "${GREEN}✅ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠️  $1${NC}"
}

print_error() {
    echo -e "${RED}❌ $1${NC}"
    exit 1
}

# Check prerequisites
check_prerequisites() {
    echo -e "${BLUE}🔍 Checking prerequisites...${NC}"
    
    # Check Docker
    if ! command -v docker &> /dev/null; then
        print_error "Docker is required but not installed"
    fi
    print_status "Docker: $(docker --version)"
    
    # Check Docker Compose
    if ! command -v docker-compose &> /dev/null; then
        print_error "Docker Compose is required but not installed"
    fi
    print_status "Docker Compose: $(docker-compose --version)"
    
    # Check Node.js
    if ! command -v node &> /dev/null; then
        print_error "Node.js is required but not installed"
    fi
    print_status "Node.js: $(node --version)"
    
    # Check .NET
    if ! command -v dotnet &> /dev/null; then
        print_error ".NET SDK is required but not installed"
    fi
    print_status ".NET SDK: $(dotnet --version)"
    
    # Check Python
    if ! command -v python3 &> /dev/null; then
        print_error "Python 3 is required but not installed"
    fi
    print_status "Python: $(python3 --version)"
    
    # Check Unity (if developing Unity components)
    if [ -d "/Applications/Unity" ] || command -v unity &> /dev/null; then
        print_status "Unity: Available"
    else
        print_warning "Unity not found - Unity development will be limited"
    fi
}

# Setup environment variables
setup_environment() {
    echo -e "${BLUE}🔧 Setting up environment variables...${NC}"
    
    # Create .env file if it doesn't exist
    if [ ! -f .env ]; then
        print_warning ".env file not found. Run security setup first:"
        echo "./scripts/setup-security.sh"
        echo
        read -p "Do you want to create a basic .env file now? (y/N): " -n 1 -r
        if [[ $REPLY =~ ^[Yy]$ ]]; then
            cat > .env << EOF
# Digital Twin Development Environment
DEPLOYMENT_ENVIRONMENT=development
DB_HOST=localhost
DB_PORT=5432
DB_NAME=digitaltwin_dev
DB_USER=devuser
DB_PASSWORD=dev_password_123
REDIS_HOST=localhost
REDIS_PORT=6379
REDIS_PASSWORD=redis_password_123
RABBITMQ_HOST=localhost
RABBITMQ_PORT=5672
RABBITMQ_USER=admin
RABBITMQ_PASSWORD=admin123
JWT_SECRET_KEY=dev_jwt_secret_key_32_chars_minimum
API_URL=http://localhost:8080
API_BASE_URL=http://localhost:8080/api
UNITY_SERVER_URL=http://localhost:8081
DEEPFACE_URL=http://localhost:8001
AVATAR_GENERATION_URL=http://localhost:8002
VOICE_SERVICE_URL=http://localhost:8003
GRAFANA_ADMIN_PASSWORD=admin123
MINIO_ROOT_USER=minioadmin
MINIO_ROOT_PASSWORD=minioadmin123
DEBUG_MODE=true
ENABLE_PROFILING=true
ENABLE_SWAGGER=true
ENABLE_API_EXPLORER=true
EOF
            print_status "Created basic .env file"
            chmod 600 .env
        fi
    else
        print_status ".env file exists"
    fi
}

# Setup Python virtual environment
setup_python_env() {
    echo -e "${BLUE}🐍 Setting up Python virtual environment...${NC}"
    
    if [ ! -d "venv" ]; then
        python3 -m venv venv
        print_status "Created Python virtual environment"
    fi
    
    source venv/bin/activate
    print_status "Activated Python virtual environment"
    
    # Install Python dependencies
    if [ -f "requirements.txt" ]; then
        pip install -r requirements.txt
        print_status "Installed Python dependencies"
    else
        print_warning "requirements.txt not found"
    fi
}

# Setup .NET projects
setup_dotnet_projects() {
    echo -e "${BLUE}🔷 Setting up .NET projects...${NC}"
    
    # Restore NuGet packages
    if [ -f "DigitalTwin.sln" ]; then
        dotnet restore DigitalTwin.sln
        print_status "Restored NuGet packages"
        
        # Build projects
        dotnet build DigitalTwin.sln --configuration Release
        print_status "Built .NET projects"
    else
        print_warning "DigitalTwin.sln not found"
    fi
}

# Setup database
setup_database() {
    echo -e "${BLUE}🗄️ Setting up database...${NC}"
    
    # Start PostgreSQL container
    if ! docker ps | grep -q digitaltwin-postgres; then
        docker-compose up -d postgres
        print_status "Started PostgreSQL container"
        
        # Wait for database to be ready
        echo "Waiting for database to be ready..."
        sleep 10
    else
        print_status "PostgreSQL already running"
    fi
    
    # Run migrations (if migration tool exists)
    if [ -f "src/API/DigitalTwin.API/Migrations/ApplicationDbContext.cs" ]; then
        dotnet ef database update --project src/API/DigitalTwin.API --startup-project src/API/DigitalTwin.API
        print_status "Applied database migrations"
    fi
}

# Setup Redis cache
setup_redis() {
    echo -e "${BLUE}⚡ Setting up Redis cache...${NC}"
    
    if ! docker ps | grep -q digitaltwin-redis; then
        docker-compose up -d redis
        print_status "Started Redis container"
    else
        print_status "Redis already running"
    fi
}

# Setup ML infrastructure
setup_ml_infrastructure() {
    echo -e "${BLUE}🤖 Setting up ML infrastructure...${NC}"
    
    # Setup ML pipeline
    if [ -f "scripts/setup-ml-pipeline.py" ]; then
        python3 scripts/setup-ml-pipeline.py
        print_status "Setup ML development pipeline"
    fi
    
    # Create directories for ML models
    mkdir -p models/{emotion-recognition,personality-models,conversation-models}
    mkdir -p data/{raw,processed,validation}
    mkdir -p training/{pipelines,configs,logs,checkpoints}
    print_status "Created ML directories"
}

# Setup monitoring
setup_monitoring() {
    echo -e "${BLUE}📊 Setting up monitoring...${NC}"
    
    # Start monitoring stack
    docker-compose up -d prometheus grafana elasticsearch kibana
    print_status "Started monitoring stack"
    
    # Wait for services to be ready
    echo "Waiting for monitoring services to be ready..."
    sleep 15
}

# Setup development tools
setup_dev_tools() {
    echo -e "${BLUE}🛠️ Setting up development tools...${NC}"
    
    # Git hooks
    cat > .git/hooks/pre-commit << 'EOF
#!/bin/sh
# Run tests before commit
dotnet test
python -m pytest tests/
EOF
    chmod +x .git/hooks/pre-commit
    print_status "Setup Git pre-commit hooks"
    
    # VS Code settings (if exists)
    if command -v code &> /dev/null; then
        mkdir -p .vscode
        cat > .vscode/settings.json << 'EOF'
{
    "python.defaultInterpreterPath": "./venv/bin/python",
    "python.terminal.activateEnvironment": "venv",
    "files.exclude": {
        "**/__pycache__": true,
        "**/node_modules": true,
        "**/bin": true,
        "**/obj": true
    },
    "editor.formatOnSave": true,
    "editor.codeActionsOnSave": {
        "source.organizeImports": true
    }
}
EOF
        print_status "Setup VS Code settings"
    fi
}

# Start development services
start_dev_services() {
    echo -e "${BLUE}🚀 Starting development services...${NC}"
    
    # Start all services
    docker-compose up -d
    
    # Wait for services to be ready
    echo "Waiting for services to be ready..."
    sleep 30
    
    # Check service health
    echo "Checking service health..."
    
    services=("api-gateway:8080" "postgres:5432" "redis:6379" "rabbitmq:5672")
    
    for service in "${services[@]}"; do
        host=$(echo $service | cut -d: -f1)
        port=$(echo $service | cut -d: -f2)
        
        if curl -f -s http://localhost:$port/health > /dev/null 2>&1; then
            print_status "$service is healthy"
        else
            print_warning "$service may not be ready yet"
        fi
    done
}

# Setup Unity development (if Unity is available)
setup_unity_dev() {
    echo -e "${BLUE}🎮 Setting up Unity development...${NC}"
    
    if [ -d "/Applications/Unity" ] || command -v unity &> /dev/null; then
        # Unity project should already exist in Assets/_Project
        print_status "Unity development environment ready"
        
        # Create Unity build configuration
        mkdir -p Build
        print_status "Created Unity Build directory"
    else
        print_warning "Unity not available - skipping Unity setup"
    fi
}

# Display development information
show_dev_info() {
    echo -e "${BLUE}📋 Development Environment Information${NC}"
    echo -e "${BLUE}======================================${NC}"
    
    echo -e "${GREEN}Local Development URLs:${NC}"
    echo "🌐 API Gateway: http://localhost:8080"
    echo "🗄️  Database: localhost:5432"
    echo "⚡ Redis: localhost:6379"
    echo "🐰 RabbitMQ Management: http://localhost:15672 (admin/admin123)"
    echo "📊 Prometheus: http://localhost:9090"
    echo "📈 Grafana: http://localhost:3000 (admin/admin123)"
    echo "🔍 Kibana: http://localhost:5601"
    
    echo -e "${GREEN}Development Commands:${NC}"
    echo "🔧 Start all services:     docker-compose up -d"
    echo "🛑 Stop all services:      docker-compose down"
    echo "📋 View logs:           docker-compose logs -f [service-name]"
    echo "🔄 Restart services:       docker-compose restart [service-name]"
    echo "🧪 Run tests:            dotnet test"
    echo "🐍 Run Python tests:      python -m pytest tests/"
    echo "📊 ML Pipeline:         python3 scripts/setup-ml-pipeline.py"
    
    echo -e "${GREEN}Useful Aliases (add to ~/.bashrc or ~/.zshrc):${NC}"
    echo "alias dt-up='cd /path/to/digitaltwin && docker-compose up -d'"
    echo "alias dt-down='cd /path/to/digitaltwin && docker-compose down'"
    echo "alias dt-logs='cd /path/to/digitaltwin && docker-compose logs -f'"
    echo "alias dt-test='cd /path/to/digitaltwin && dotnet test'"
}

# Main execution
main() {
    echo -e "${YELLOW}⚠️  This will set up the complete Digital Twin development environment${NC}"
    echo -e "${YELLOW}⚠️  Make sure you have Docker, Docker Compose, Node.js, .NET, and Python installed${NC}"
    echo
    read -p "Continue with setup? (y/N): " -n 1 -r
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        echo "Setup cancelled."
        exit 0
    fi
    
    echo
    echo -e "${BLUE}🚀 Starting Digital Twin development environment setup...${NC}"
    echo
    
    check_prerequisites
    setup_environment
    setup_python_env
    setup_dotnet_projects
    setup_database
    setup_redis
    setup_ml_infrastructure
    setup_monitoring
    setup_dev_tools
    setup_unity_dev
    start_dev_services
    show_dev_info
    
    echo
    echo -e "${GREEN}🎉 Development environment setup completed successfully!${NC}"
    echo -e "${GREEN}🚀 Ready to start building the premier emotional companion!${NC}"
    echo
    echo -e "${BLUE}Next steps:${NC}"
    echo "1. Review IMPLEMENTATION_ROADMAP.md for Phase 1 tasks"
    echo "2. Start with Sprint 1: Voice Emotion Detection"
    echo "3. Run tests: 'dotnet test' and 'python -m pytest tests/'"
    echo "4. Commit changes and push to trigger CI/CD pipeline"
}

# Run main function
main "$@"
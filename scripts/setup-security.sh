#!/bin/bash

# Digital Twin Security Setup Script
# This script helps generate secure configuration values

set -e

echo "🔒 Digital Twin Security Setup"
echo "============================"
echo

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to generate secure random string
generate_secure_string() {
    local length=${1:-32}
    openssl rand -base64 $length | tr -d "=+/" | cut -c1-$length
}

# Function to check if OpenSSL is available
check_openssl() {
    if ! command -v openssl &> /dev/null; then
        echo -e "${RED}Error: OpenSSL is required but not installed. Please install OpenSSL first.${NC}"
        exit 1
    fi
}

# Function to validate password strength
validate_password() {
    local password=$1
    local name=$2
    
    if [ ${#password} -lt 12 ]; then
        echo -e "${RED}Error: $name must be at least 12 characters long${NC}"
        return 1
    fi
    
    if [[ ! $password =~ [A-Z] ]]; then
        echo -e "${RED}Error: $name must contain at least one uppercase letter${NC}"
        return 1
    fi
    
    if [[ ! $password =~ [a-z] ]]; then
        echo -e "${RED}Error: $name must contain at least one lowercase letter${NC}"
        return 1
    fi
    
    if [[ ! $password =~ [0-9] ]]; then
        echo -e "${RED}Error: $name must contain at least one digit${NC}"
        return 1
    fi
    
    if [[ ! $password =~ [^a-zA-Z0-9] ]]; then
        echo -e "${RED}Error: $name must contain at least one special character${NC}"
        return 1
    fi
    
    return 0
}

echo -e "${YELLOW}This script will generate secure configuration values for your Digital Twin application.${NC}"
echo -e "${YELLOW}All generated values will be saved to a new .env file.${NC}"
echo

read -p "Do you want to proceed? (y/N): " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "Setup cancelled."
    exit 0
fi

check_openssl

# Generate JWT secret key
echo "🔑 Generating JWT secret key..."
JWT_SECRET=$(generate_secure_string 32)
echo -e "${GREEN}✓ JWT secret key generated${NC}"

# Generate database credentials
echo "🗄️  Generating database credentials..."
DB_USER="dtadmin_$(openssl rand -hex 4)"
DB_PASSWORD=$(generate_secure_string 24)
validate_password "$DB_PASSWORD" "Database password"
echo -e "${GREEN}✓ Database credentials generated${NC}"

# Generate Redis password
echo "⚡ Generating Redis password..."
REDIS_PASSWORD=$(generate_secure_string 24)
validate_password "$REDIS_PASSWORD" "Redis password"
echo -e "${GREEN}✓ Redis password generated${NC}"

# Generate RabbitMQ credentials
echo "🐰 Generating RabbitMQ credentials..."
RABBITMQ_USER="admin_$(openssl rand -hex 4)"
RABBITMQ_PASSWORD=$(generate_secure_string 24)
validate_password "$RABBITMQ_PASSWORD" "RabbitMQ password"
echo -e "${GREEN}✓ RabbitMQ credentials generated${NC}"

# Generate Grafana password
echo "📊 Generating Grafana password..."
GRAFANA_ADMIN_PASSWORD=$(generate_secure_string 20)
validate_password "$GRAFANA_ADMIN_PASSWORD" "Grafana password"
echo -e "${GREEN}✓ Grafana password generated${NC}"

# Generate MinIO credentials
echo "📦 Generating MinIO credentials..."
MINIO_ROOT_USER="admin_$(openssl rand -hex 4)"
MINIO_ROOT_PASSWORD=$(generate_secure_string 24)
validate_password "$MINIO_ROOT_PASSWORD" "MinIO password"
echo -e "${GREEN}✓ MinIO credentials generated${NC}"

# Create .env file
echo
echo "📝 Creating .env file..."

cat > .env << EOF
# Digital Twin Production Environment Variables
# Generated on $(date)

# Environment
DEPLOYMENT_ENVIRONMENT=production

# AWS Configuration
AWS_REGION=us-west-2
AWS_ACCESS_KEY_ID=CHANGE_THIS_AWS_ACCESS_KEY
AWS_SECRET_ACCESS_KEY=CHANGE_THIS_AWS_SECRET_KEY

# Database Configuration
DB_HOST=localhost
DB_PORT=5432
DB_NAME=digitaltwin_prod
DB_USER=$DB_USER
DB_PASSWORD=$DB_PASSWORD
DB_UID=999
DB_GID=999
CONNECTION_STRING=Server=\${DB_HOST};Port=\${DB_PORT};Database=\${DB_NAME};User Id=\${DB_USER};Password=\${DB_PASSWORD};

# Redis Configuration
REDIS_HOST=localhost
REDIS_PORT=6379
REDIS_PASSWORD=$REDIS_PASSWORD
REDIS_CONNECTION_STRING=\${REDIS_HOST}:\${REDIS_PORT},password=\${REDIS_PASSWORD}

# RabbitMQ Configuration
RABBITMQ_HOST=localhost
RABBITMQ_PORT=5672
RABBITMQ_USER=$RABBITMQ_USER
RABBITMQ_PASSWORD=$RABBITMQ_PASSWORD
RABBITMQ_VIRTUAL_HOST=digitaltwin
RABBITMQ_CONNECTION_STRING=amqp://\${RABBITMQ_USER}:\${RABBITMQ_PASSWORD}@\${RABBITMQ_HOST}:\${RABBITMQ_PORT}/\${RABBITMQ_VIRTUAL_HOST}

# JWT Configuration
JWT_SECRET_KEY=$JWT_SECRET
JWT_ISSUER=DigitalTwin
JWT_AUDIENCE=DigitalTwin
JWT_EXPIRY_MINUTES=60

# API Configuration
API_URL=https://api.digitaltwin.com
API_BASE_URL=https://api.digitaltwin.com/api
CORS_ALLOWED_ORIGINS=https://digitaltwin.com,https://app.digitaltwin.com

# External Services Configuration
SLACK_WEBHOOK=CHANGE_THIS_SLACK_WEBHOOK
TEAMS_WEBHOOK=CHANGE_THIS_TEAMS_WEBHOOK
NOTIFICATION_EMAIL=notifications@digitaltwin.com

# Monitoring Configuration
PROMETHEUS_URL=https://prometheus.digitaltwin.com
GRAFANA_URL=https://grafana.digitaltwin.com
ELASTICSEARCH_URL=https://elasticsearch.digitaltwin.com
KIBANA_URL=https://kibana.digitaltwin.com

# Unity Configuration
UNITY_SERVER_URL=https://unity.digitaltwin.com
UNITY_BUILD_PATH=./Builds/WebGL
UNITY_ASSETS_PATH=./Assets

# Avatar Twin Services Configuration
DEEPFACE_URL=https://deepface.digitaltwin.com
AVATAR_GENERATION_URL=https://avatar.digitaltwin.com

# Voice Service Configuration
VOICE_SERVICE_URL=https://voice.digitaltwin.com
ELEVENLABS_API_KEY=CHANGE_THIS_ELEVENLABS_API_KEY

# File Storage Configuration
UPLOAD_PATH=./uploads
MAX_FILE_SIZE=10485760
ALLOWED_FILE_TYPES=.jpg,.jpeg,.png,.pdf,.csv,.xlsx

# Logging Configuration
LOG_LEVEL=Warning
LOG_PATH=./logs
LOG_RETENTION_DAYS=90
LOG_FORMAT=json

# Cache Configuration
CACHE_DURATION_SECONDS=300
CACHE_MAX_SIZE=1000

# Performance Configuration
MAX_CONCURRENT_REQUESTS=1000
REQUEST_TIMEOUT_SECONDS=30
ENABLE_RESPONSE_COMPRESSION=true

# Security Configuration
ENABLE_HTTPS=true
SSL_CERT_PATH=./ssl/cert.pem
SSL_KEY_PATH=./ssl/key.pem
RATE_LIMIT_REQUESTS_PER_MINUTE=100
ENABLE_RATE_LIMITING=true

# Development Tools Configuration
DEBUG_MODE=false
ENABLE_PROFILING=false
ENABLE_SWAGGER=false
ENABLE_API_EXPLORER=false

# CI/CD Configuration
GITHUB_TOKEN=CHANGE_THIS_GITHUB_TOKEN
DOCKER_REGISTRY=your-registry.com
SONAR_TOKEN=CHANGE_THIS_SONAR_TOKEN
CODECOV_TOKEN=CHANGE_THIS_CODECOV_TOKEN

# Additional Service Configuration (for docker-compose)
GRAFANA_ADMIN_PASSWORD=$GRAFANA_ADMIN_PASSWORD
MINIO_ROOT_USER=$MINIO_ROOT_USER
MINIO_ROOT_PASSWORD=$MINIO_ROOT_PASSWORD
EOF

echo -e "${GREEN}✓ .env file created successfully${NC}"

# Set secure file permissions
chmod 600 .env
echo -e "${GREEN}✓ File permissions set to 600 (read/write for owner only)${NC}"

echo
echo "🎉 Setup completed!"
echo
echo -e "${YELLOW}IMPORTANT SECURITY NOTES:${NC}"
echo "1. Store the .env file securely and never commit it to version control"
echo "2. Add .env to your .gitignore file"
echo "3. Change all CHANGE_THIS_* values with your actual credentials"
echo "4. Use a secrets management system in production (AWS Secrets Manager, Azure Key Vault, etc.)"
echo "5. Regularly rotate your secrets"
echo "6. Enable file encryption for sensitive files"
echo
echo -e "${YELLOW}Next steps:${NC}"
echo "1. Review the generated .env file"
echo "2. Update any CHANGE_THIS_* values"
echo "3. Test your application with the new configuration"
echo "4. Set up monitoring for security events"
echo
echo "📋 Summary of generated credentials (for your reference):"
echo "Database User: $DB_USER"
echo "RabbitMQ User: $RABBITMQ_USER"
echo "MinIO User: $MINIO_ROOT_USER"
echo
echo "🔐 Keep this information secure and store it in a password manager."
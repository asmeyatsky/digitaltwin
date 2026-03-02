#!/bin/bash
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"

cd "$PROJECT_DIR"

echo "=== Digital Twin: Web Dev Setup ==="

if [ ! -f "$PROJECT_DIR/web/.env.local" ]; then
  echo "Creating web/.env.local..."
  cat > "$PROJECT_DIR/web/.env.local" << 'EOF'
NEXT_PUBLIC_API_URL=http://localhost:5002
EOF
fi

# Start backend services
echo "Starting backend services..."
docker compose -f docker-compose.dev.yml up -d postgres-dev redis-dev

echo "Waiting for database to be ready..."
sleep 5

# Check if API image exists, build if needed
if ! docker image ls digitaltwin-api-dev -q > /dev/null 2>&1; then
  echo "Building API image..."
  docker compose -f docker-compose.dev.yml build api-dev
fi

# Start API
echo "Starting API..."
docker compose -f docker-compose.dev.yml up -d api-dev

# Wait for API health
echo "Waiting for API to be healthy..."
for i in {1..30}; do
  if curl -sf http://localhost:5002/health > /dev/null 2>&1; then
    echo "API is healthy!"
    break
  fi
  echo "Waiting for API... ($i/30)"
  sleep 2
done

# Install web dependencies if needed
if [ ! -d "$PROJECT_DIR/web/node_modules" ]; then
  echo "Installing web dependencies..."
  cd "$PROJECT_DIR/web" && npm install
fi

# Start web
echo "Starting web dev server..."
cd "$PROJECT_DIR/web" && npm run dev &
WEB_PID=$!

echo ""
echo "=== Ready! ==="
echo "Web:   http://localhost:3000"
echo "API:   http://localhost:5000"
echo "PG:    localhost:5433"
echo "Redis: localhost:6380"
echo ""
echo "Press Ctrl+C to stop all services"

# Handle cleanup
trap "docker compose -f docker-compose.dev.yml down; kill $WEB_PID 2>/dev/null" EXIT

wait $WEB_PID

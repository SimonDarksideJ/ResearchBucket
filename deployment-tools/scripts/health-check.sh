#!/usr/bin/env bash

################################################################################
# Health Check Script for LiveKit Deployment
#
# Checks the health of all services and provides detailed status report
#
# Usage: ./health-check.sh [OPTIONS]
#
# Options:
#   --verbose    Show detailed output
#   --json       Output in JSON format
#   --help       Show help
#
################################################################################

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

VERBOSE=false
JSON_OUTPUT=false

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --verbose) VERBOSE=true; shift ;;
        --json) JSON_OUTPUT=true; shift ;;
        --help)
            grep '^#' "$0" | sed 's/^# //' | sed 's/^#//'
            exit 0
            ;;
        *) echo "Unknown option: $1"; exit 1 ;;
    esac
done

# Find LiveKit installation
if [[ -L "$HOME/livekit" ]]; then
    LIVEKIT_DIR="$HOME/livekit"
elif [[ -d "/opt/livekit" ]]; then
    LIVEKIT_DIR="/opt/livekit"
else
    echo "❌ LiveKit installation not found"
    exit 1
fi

cd "$LIVEKIT_DIR"

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "  LiveKit Health Check"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

# Check Docker
echo "🐳 Docker Status:"
if docker info &>/dev/null; then
    echo "  ✅ Docker is running"
else
    echo "  ❌ Docker is not running"
    exit 1
fi
echo ""

# Check Services
echo "📦 Service Status:"
SERVICES=("livekit" "caddy" "prometheus" "grafana" "loki" "promtail" "node-exporter")
ALL_HEALTHY=true

for service in "${SERVICES[@]}"; do
    if docker compose ps "$service" | grep -q "Up"; then
        echo "  ✅ $service"
    else
        echo "  ❌ $service"
        ALL_HEALTHY=false
    fi
done
echo ""

# Check Endpoints
echo "🌐 Endpoint Health:"
check_endpoint() {
    local name=$1
    local url=$2
    local expected=$3
    
    if curl -sf "$url" > /dev/null 2>&1; then
        echo "  ✅ $name: $url"
        return 0
    else
        echo "  ❌ $name: $url (unreachable)"
        return 1
    fi
}

check_endpoint "LiveKit" "http://localhost:7880/" "OK"
check_endpoint "Grafana" "http://localhost:3000/api/health" "OK"
check_endpoint "Prometheus" "http://localhost:9090/-/healthy" "OK"
check_endpoint "Loki" "http://localhost:3100/ready" "OK"
echo ""

# Check Resources
echo "💻 Resource Usage:"
docker stats --no-stream --format "table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}" | head -8
echo ""

# Check Disk Space
echo "💾 Storage Status:"
if [[ "$OSTYPE" == "darwin"* ]]; then
    df -h "$LIVEKIT_DIR" | tail -1 | awk '{print "  Volume: " $9 "\n  Used: " $3 " / " $2 " (" $5 ")"}'
else
    df -h "$LIVEKIT_DIR" | tail -1 | awk '{print "  Used: " $3 " / " $2 " (" $5 ")"}'
fi
echo ""

# Check Logs for Errors
echo "📋 Recent Errors:"
ERROR_COUNT=$(docker compose logs --since=1h 2>&1 | grep -i "error" | wc -l | tr -d ' ')
if [[ $ERROR_COUNT -eq 0 ]]; then
    echo "  ✅ No errors in last hour"
else
    echo "  ⚠️  $ERROR_COUNT errors found in last hour"
    if [[ "$VERBOSE" == true ]]; then
        docker compose logs --since=1h 2>&1 | grep -i "error" | tail -10
    fi
fi
echo ""

# Summary
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
if [[ "$ALL_HEALTHY" == true ]] && [[ $ERROR_COUNT -eq 0 ]]; then
    echo "✅ All systems healthy!"
else
    echo "⚠️  Some issues detected - check details above"
fi
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
